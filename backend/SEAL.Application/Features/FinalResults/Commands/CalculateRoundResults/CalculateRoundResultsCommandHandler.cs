// [FLOW3-CHAMDIEM][CalculateRoundResults] Tinh diem trung binh va xep hang cac doi thi trong 1 vong thi.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults
{
    public class CalculateRoundResultsCommandHandler : IRequestHandler<CalculateRoundResultsCommand, Result<List<CalculateRoundResultItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CalculateRoundResultsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<CalculateRoundResultItemModel>>> Handle(CalculateRoundResultsCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra tồn tại Vòng thi
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.RoundId);
            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.RoundId}' không tồn tại.");
            }

            // 2. CHỈ ĐƯỢC TÍNH KHI VÒNG ĐÃ KẾT THÚC
            if (System.DateTime.UtcNow <= round.EndDate)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vòng thi chưa kết thúc (chưa hết hạn nộp/chấm bài) nên chưa thể tính kết quả.");
            }

            // 2b. ĐÃ CÔNG BỐ KẾT QUẢ -> KHÓA TÍNH LẠI: kết quả đã công bố là quyết định chính thức,
            //     tính lại sẽ ghi đèIsPublished=false làm ẩn kết quả khỏi thí sinh/bảng xếp hạng public.
            var isPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.RoundId == request.RoundId && fr.IsPublished, cancellationToken);
            if (isPublished)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Kết quả vòng thi này đã được CÔNG BỐ CHÍNH THỨC nên không thể tính lại.");
            }

            // 2c. KHÔNG tính lại khi VÒNG SAU đã vận hành dựa trên kết quả hiện tại
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

            // 3. Lấy tất cả bài nộp trong vòng thi này
            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .Where(s => s.RoundId == request.RoundId && s.IsActive)
                .ToListAsync(cancellationToken);

            if (submissions.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi chưa có bài nộp nào để tính kết quả.");
            }

            // 4. Lấy các phiếu chấm của các bài nộp trong vòng
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var judgeRoleIds = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Where(er => er.EventId == round.EventId && er.RoleName == EventRoleType.Judge)
                .Select(er => er.Id)
                .ToListAsync(cancellationToken);
            var scores = await _unitOfWork.GetRepository<Score>().Entities
                .Where(sc => submissionIds.Contains(sc.SubmitResultId) && judgeRoleIds.Contains(sc.EventRoleId))
                .ToListAsync(cancellationToken);

            // 4b. Kiểm tra lượt chấm
            var trackIds = submissions.Select(s => s.TrackId).Distinct().ToList();
            var nowUtc = System.DateTime.UtcNow;
            var judgeRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Where(er => er.RoleName == EventRoleType.Judge && er.TrackId != null && trackIds.Contains(er.TrackId)
                          && (er.ExpiredAt == null || er.ExpiredAt > nowUtc))
                .ToListAsync(cancellationToken);
            var scoredPairs = scores.Select(sc => sc.EventRoleId + "|" + sc.SubmitResultId).ToHashSet();
            int missingCount = 0;
            foreach (var sub in submissions)
            {
                var trackJudges = judgeRoles.Where(j => j.TrackId == sub.TrackId).ToList();
                if (trackJudges.Count == 0)
                {
                    missingCount++;
                    continue;
                }
                missingCount += trackJudges.Count(j => !scoredPairs.Contains(j.Id + "|" + sub.Id));
            }
            if (missingCount > 0)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    $"Chưa thể tính kết quả: còn {missingCount} lượt chấm chưa hoàn tất. Vui lòng đợi tất cả giám khảo chấm xong.");
            }

            // 5. Tính FinalScore mỗi đội
            var allRoundTracks = await _unitOfWork.GetRepository<Track>().GetQueryable()
                .AsNoTracking()
                .Where(t => t.EventId == round.EventId)
                .ToListAsync(cancellationToken);

            var teamFinalScores = submissions
                .Select(s => s.TeamId)
                .Distinct()
                .Select(teamId =>
                {
                    var perTrackScores = new List<decimal>();
                    foreach (var trk in allRoundTracks)
                    {
                        var sub = submissions.FirstOrDefault(s => s.TeamId == teamId && s.TrackId == trk.Id);
                        if (sub != null)
                        {
                            var subScores = scores.Where(sc => sc.SubmitResultId == sub.Id).ToList();
                            if (subScores.Count > 0)
                            {
                                perTrackScores.Add(subScores.Average(sc => sc.TotalScore));
                            }
                        }
                        else
                        {
                            var trackEffectiveEnd = trk.EndDate ?? round.EndDate;
                            if (nowUtc > trackEffectiveEnd)
                            {
                                perTrackScores.Add(0m);
                            }
                        }
                    }

                    decimal finalScore = perTrackScores.Count > 0
                        ? System.Math.Round(perTrackScores.Average(), 2, System.MidpointRounding.AwayFromZero)
                        : 0m;
                    return new { TeamId = teamId, FinalScore = finalScore };
                })
                .OrderByDescending(x => x.FinalScore)
                .ThenBy(x => x.TeamId)
                .ToList();

            // 7. Xóa kết quả cũ của vòng
            var oldResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(f => f.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);
            if (oldResults.Count > 0)
            {
                await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(oldResults);
            }

            int totalTeams = teamFinalScores.Count;
            int? cutoffRank = null;
            decimal? minScore = null;
            if (TryParseAdvancementRule(round.AdvancementRule, out var ruleType, out var ruleValue))
            {
                switch (ruleType)
                {
                    case "top": cutoffRank = (int)ruleValue; break;
                    case "percent": cutoffRank = (int)System.Math.Ceiling(totalTeams * ruleValue / 100m); break;
                    case "minscore": minScore = ruleValue; break;
                    default: cutoffRank = request.TopN; break;
                }
            }
            else
            {
                cutoffRank = request.TopN;
            }

            var resultModels = new List<CalculateRoundResultItemModel>();
            int rank = 0;
            int position = 0;
            decimal? previousScore = null;
            foreach (var item in teamFinalScores)
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
                    TeamId = finalResult.TeamId,
                    RoundId = finalResult.RoundId,
                    EventId = finalResult.EventId,
                    FinalScore = finalResult.FinalScore,
                    Rank = finalResult.Rank,
                    IsAdvanced = finalResult.IsAdvanced,
                    IsPublished = finalResult.IsPublished
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return resultModels;
        }

        private static bool TryParseAdvancementRule(string? ruleStr, out string type, out decimal val)
        {
            type = string.Empty;
            val = 0m;
            if (string.IsNullOrWhiteSpace(ruleStr)) return false;
            var parts = ruleStr.Trim().Split(':');
            if (parts.Length != 2) return false;
            type = parts[0].Trim().ToLowerInvariant();
            return decimal.TryParse(parts[1].Trim(), out val);
        }
    }
}
