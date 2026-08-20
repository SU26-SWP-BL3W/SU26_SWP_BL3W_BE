// [FLOW3-CHAMDIEM][CalculateRoundResults] Xep hang Top N theo tung hang muc, khong trung binh cheo Track.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults
{
    public class CalculateRoundResultsCommandHandler : IRequestHandler<CalculateRoundResultsCommand, Result<List<CalculateRoundResultItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;

        public CalculateRoundResultsCommandHandler(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
        }

        public async Task<Result<List<CalculateRoundResultItemModel>>> Handle(CalculateRoundResultsCommand request, CancellationToken cancellationToken)
        {
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.RoundId);
            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.RoundId}' không tồn tại.");
            }

            if (System.DateTime.UtcNow <= round.EndDate)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vòng thi chưa kết thúc (chưa hết hạn nộp/chấm bài) nên chưa thể tính kết quả.");
            }

            var isPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.RoundId == request.RoundId && fr.IsPublished, cancellationToken);
            if (isPublished)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Kết quả vòng thi này đã được CÔNG BỐ CHÍNH THỨC nên không thể tính lại.");
            }

            var laterRoundIds = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .Where(r => r.EventId == round.EventId && r.RoundNumber > round.RoundNumber)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);
            if (laterRoundIds.Count > 0)
            {
                var laterHasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                    sr => laterRoundIds.Contains(sr.RoundId), cancellationToken);
                var laterHasResults = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId != null && laterRoundIds.Contains(fr.RoundId), cancellationToken);
                if (laterHasSubmissions || laterHasResults)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Vòng sau đã có bài nộp/kết quả dựa trên kết quả vòng này nên không thể tính lại. Vui lòng xử lý dữ liệu vòng sau trước.");
                }
            }

            var tracks = await _unitOfWork.GetRepository<Track>().GetQueryable()
                .AsNoTracking()
                .Where(t => t.EventId == round.EventId)
                .ToListAsync(cancellationToken);
            if (tracks.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse("Sự kiện chưa có hạng mục thi để tính kết quả.");
            }

            var teams = await _unitOfWork.GetRepository<Team>().GetQueryable()
                .AsNoTracking()
                .Where(t => t.EventId == round.EventId && t.Status == TeamStatus.Registered && t.TrackId != null)
                .ToListAsync(cancellationToken);

            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .Where(s => s.RoundId == request.RoundId && s.IsActive)
                .ToListAsync(cancellationToken);

            var registeredTeamIds = teams.Select(t => t.Id).ToHashSet();
            submissions = submissions.Where(s => registeredTeamIds.Contains(s.TeamId)).ToList();

            var submissionIds = submissions.Select(s => s.Id).ToList();
            var nowUtc = System.DateTime.UtcNow;
            var judgeRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Include(er => er.Event)
                .Where(er => er.EventId == round.EventId
                          && er.RoleName == EventRoleType.Judge
                          && er.TrackId != null
                          && (er.ExpiredAt ?? er.Event.EndDate) > nowUtc)
                .ToListAsync(cancellationToken);
            var judgeRoleIds = judgeRoles.Select(j => j.Id).ToHashSet();

            // Chỉ phiếu đã chốt: draft không được tính là đủ phiếu và không vào điểm TB.
            var scores = submissionIds.Count == 0
                ? new List<Score>()
                : await _unitOfWork.GetRepository<Score>().Entities
                    .Where(sc => submissionIds.Contains(sc.SubmitResultId)
                              && judgeRoleIds.Contains(sc.EventRoleId)
                              && sc.IsSubmitted)
                    .ToListAsync(cancellationToken);
            var scoredPairs = scores.Select(sc => sc.EventRoleId + "|" + sc.SubmitResultId).ToHashSet();

            var legacyDrafts = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(f => f.RoundId == request.RoundId && f.TrackId == null && !f.IsPublished)
                .ToListAsync(cancellationToken);
            if (legacyDrafts.Count > 0)
            {
                await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(legacyDrafts);
            }

            var resultModels = new List<CalculateRoundResultItemModel>();
            var skipped = new List<string>();

            foreach (var track in tracks)
            {
                var trackTeams = teams.Where(t => t.TrackId == track.Id).ToList();
                var trackSubs = submissions.Where(s => s.TrackId == track.Id).ToList();
                if (trackTeams.Count == 0 && trackSubs.Count == 0)
                {
                    continue;
                }

                var trackJudges = judgeRoles.Where(j => j.TrackId == track.Id).ToList();
                int missingCount = 0;
                foreach (var sub in trackSubs)
                {
                    if (trackJudges.Count == 0)
                    {
                        missingCount++;
                        continue;
                    }
                    missingCount += trackJudges.Count(j => !scoredPairs.Contains(j.Id + "|" + sub.Id));
                }
                if (missingCount > 0)
                {
                    skipped.Add($"{track.TrackName} (thiếu {missingCount} phiếu)");
                    continue;
                }

                var teamScores = trackTeams
                    .Select(team =>
                    {
                        var sub = trackSubs.FirstOrDefault(s => s.TeamId == team.Id);
                        decimal score = 0m;
                        if (sub != null)
                        {
                            var subScores = scores.Where(sc => sc.SubmitResultId == sub.Id).ToList();
                            if (subScores.Count > 0)
                            {
                                score = subScores.Average(sc => sc.TotalScore);
                            }
                        }
                        return new
                        {
                            TeamId = team.Id,
                            FinalScore = System.Math.Round(score, 2, System.MidpointRounding.AwayFromZero)
                        };
                    })
                    .OrderByDescending(x => x.FinalScore)
                    .ThenBy(x => x.TeamId)
                    .ToList();

                if (teamScores.Count == 0)
                {
                    continue;
                }

                ParseAdvancement(round.AdvancementRule, request.TopN, teamScores.Count, out var cutoffRank, out var minScore);

                var oldTrackResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                    .Where(f => f.RoundId == request.RoundId && f.TrackId == track.Id && !f.IsPublished)
                    .ToListAsync(cancellationToken);
                if (oldTrackResults.Count > 0)
                {
                    await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(oldTrackResults);
                }

                int rank = 0;
                int position = 0;
                decimal? previousScore = null;
                foreach (var item in teamScores)
                {
                    position++;
                    if (previousScore == null || item.FinalScore != previousScore.Value)
                    {
                        rank = position;
                        previousScore = item.FinalScore;
                    }

                    var finalResult = new FinalResult
                    {
                        TeamId = item.TeamId,
                        RoundId = request.RoundId,
                        EventId = round.EventId,
                        TrackId = track.Id,
                        FinalScore = item.FinalScore,
                        Rank = rank,
                        IsAdvanced = minScore.HasValue
                            ? item.FinalScore >= minScore.Value
                            : rank <= cutoffRank!.Value,
                        IsPublished = false
                    };
                    await _unitOfWork.GetRepository<FinalResult>().AddAsync(finalResult);

                    resultModels.Add(new CalculateRoundResultItemModel
                    {
                        Id = finalResult.Id,
                        FinalResultId = finalResult.Id,
                        TeamId = finalResult.TeamId,
                        RoundId = finalResult.RoundId,
                        EventId = finalResult.EventId,
                        TrackId = finalResult.TrackId,
                        FinalScore = finalResult.FinalScore,
                        Rank = finalResult.Rank,
                        IsAdvanced = finalResult.IsAdvanced,
                        IsPublished = finalResult.IsPublished
                    });
                }
            }

            if (resultModels.Count == 0)
            {
                var detail = skipped.Count > 0
                    ? " Các hạng mục bỏ qua: " + string.Join("; ", skipped) + "."
                    : string.Empty;
                return BaseException.BadRequestInvaildInputResponse(
                    "Chưa tính được hạng mục nào (thiếu bài nộp hoặc chưa đủ phiếu chấm)." + detail);
            }

            await _auditLogService.AppendAsync(
                AuditActions.CalculateRoundResults,
                AuditEntityTypes.Round,
                round.Id,
                round.EventId,
                $"Tính kết quả vòng {round.RoundName}: {resultModels.Count} đội",
                new { count = resultModels.Count, skipped },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return resultModels;
        }

        private static void ParseAdvancement(
            string? ruleStr,
            int fallbackTopN,
            int teamCount,
            out int? cutoffRank,
            out decimal? minScore)
        {
            cutoffRank = null;
            minScore = null;
            if (TryParseAdvancementRule(ruleStr, out var ruleType, out var ruleValue))
            {
                switch (ruleType)
                {
                    case "top":
                        cutoffRank = (int)ruleValue;
                        break;
                    case "percent":
                        cutoffRank = (int)System.Math.Ceiling(teamCount * ruleValue / 100m);
                        break;
                    case "minscore":
                        minScore = ruleValue;
                        break;
                    default:
                        cutoffRank = fallbackTopN;
                        break;
                }
            }
            else
            {
                cutoffRank = fallbackTopN;
            }
        }

        private static bool TryParseAdvancementRule(string? ruleStr, out string type, out decimal val)
        {
            type = string.Empty;
            val = 0m;
            if (string.IsNullOrWhiteSpace(ruleStr)) return false;
            var parts = ruleStr.Trim().Split(':', 2);
            if (parts.Length != 2) return false;
            type = parts[0].Trim().ToLowerInvariant();
            return decimal.TryParse(
                parts[1].Trim(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out val);
        }
    }
}
