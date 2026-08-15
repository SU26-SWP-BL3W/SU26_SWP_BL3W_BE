// [FLOW3-CHAMDIEM][ExportScoresCsv] CSV diem tung tieu chi; mac dinh an danh phuc vu RBL.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Queries.ExportScoresCsv
{
    public class ExportScoresCsvQueryHandler : IRequestHandler<ExportScoresCsvQuery, Result<byte[]>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public ExportScoresCsvQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<byte[]>> Handle(ExportScoresCsvQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện '{request.EventId}' không tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, request.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên được xuất dữ liệu chấm.");
            }

            var trackIds = await _unitOfWork.GetRepository<Track>().GetQueryable()
                .AsNoTracking()
                .Where(t => t.EventId == request.EventId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .AsNoTracking()
                .Include(s => s.Team)
                .Where(s => trackIds.Contains(s.TrackId) && s.IsActive)
                .ToListAsync(cancellationToken);

            var submissionIds = submissions.Select(s => s.Id).ToList();
            var scores = submissionIds.Count == 0
                ? new List<Score>()
                : await _unitOfWork.GetRepository<Score>().Entities
                    .AsNoTracking()
                    .Include(sc => sc.ScoreDetails)
                    .Include(sc => sc.EventRole).ThenInclude(er => er.User)
                    .Where(sc => submissionIds.Contains(sc.SubmitResultId))
                    .ToListAsync(cancellationToken);

            var criteriaIds = scores.SelectMany(s => s.ScoreDetails).Select(d => d.CriteriaId).Distinct().ToList();
            var criteriaNames = criteriaIds.Count == 0
                ? new Dictionary<string, string>()
                : await _unitOfWork.GetRepository<Criteria>().Entities
                    .AsNoTracking()
                    .Where(c => criteriaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.CriteriaName, cancellationToken);

            var templateIds = scores.SelectMany(s => s.ScoreDetails).Select(d => d.TemplateId).Distinct().ToList();
            var templateConfigs = templateIds.Count == 0
                ? new List<TemplateCriteria>()
                : await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                    .AsNoTracking()
                    .Where(tc => templateIds.Contains(tc.TemplateId))
                    .ToListAsync(cancellationToken);
            var configMap = templateConfigs.ToDictionary(tc => tc.TemplateId + "|" + tc.CriteriaId, tc => tc);

            var teamAlias = submissions.Select(s => s.TeamId).Distinct().OrderBy(id => id)
                .Select((id, i) => (id, alias: "T" + (i + 1)))
                .ToDictionary(x => x.id, x => x.alias);
            var judgeAlias = scores.Select(s => s.EventRoleId).Distinct().OrderBy(id => id)
                .Select((id, i) => (id, alias: "J" + (i + 1)))
                .ToDictionary(x => x.id, x => x.alias);

            var sb = new StringBuilder();
            if (request.Anonymize)
            {
                sb.AppendLine("EventId,RoundId,TrackId,TeamAlias,JudgeAlias,CriteriaId,CriteriaName,Value,MaxScore,Weight,TotalScore,IsSubmitted");
            }
            else
            {
                sb.AppendLine("EventId,RoundId,TrackId,TeamId,TeamName,JudgeId,JudgeName,CriteriaId,CriteriaName,Value,MaxScore,Weight,TotalScore,IsSubmitted");
            }

            var subMap = submissions.ToDictionary(s => s.Id);
            foreach (var score in scores.OrderBy(s => s.EventRoleId).ThenBy(s => s.SubmitResultId))
            {
                if (!subMap.TryGetValue(score.SubmitResultId, out var sub)) continue;
                foreach (var detail in score.ScoreDetails.OrderBy(d => d.CriteriaId))
                {
                    configMap.TryGetValue(detail.TemplateId + "|" + detail.CriteriaId, out var cfg);
                    var criteriaName = criteriaNames.TryGetValue(detail.CriteriaId, out var n) ? n : detail.CriteriaId;
                    if (request.Anonymize)
                    {
                        sb.AppendLine(string.Join(",",
                            Csv(request.EventId),
                            Csv(sub.RoundId),
                            Csv(sub.TrackId),
                            Csv(teamAlias.GetValueOrDefault(sub.TeamId, sub.TeamId)),
                            Csv(judgeAlias.GetValueOrDefault(score.EventRoleId, score.EventRoleId)),
                            Csv(detail.CriteriaId),
                            Csv(criteriaName),
                            Csv(detail.Value.ToString(CultureInfo.InvariantCulture)),
                            Csv((cfg?.MaxScore ?? 0m).ToString(CultureInfo.InvariantCulture)),
                            Csv((cfg?.Weight ?? 0m).ToString(CultureInfo.InvariantCulture)),
                            Csv(score.TotalScore.ToString(CultureInfo.InvariantCulture)),
                            score.IsSubmitted ? "true" : "false"));
                    }
                    else
                    {
                        sb.AppendLine(string.Join(",",
                            Csv(request.EventId),
                            Csv(sub.RoundId),
                            Csv(sub.TrackId),
                            Csv(sub.TeamId),
                            Csv(sub.Team?.Name ?? string.Empty),
                            Csv(score.EventRoleId),
                            Csv(score.EventRole?.User?.FullName ?? string.Empty),
                            Csv(detail.CriteriaId),
                            Csv(criteriaName),
                            Csv(detail.Value.ToString(CultureInfo.InvariantCulture)),
                            Csv((cfg?.MaxScore ?? 0m).ToString(CultureInfo.InvariantCulture)),
                            Csv((cfg?.Weight ?? 0m).ToString(CultureInfo.InvariantCulture)),
                            Csv(score.TotalScore.ToString(CultureInfo.InvariantCulture)),
                            score.IsSubmitted ? "true" : "false"));
                    }
                }
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
            return bytes;
        }

        private static string Csv(string? value)
        {
            var v = value ?? string.Empty;
            if (v.Contains('"') || v.Contains(',') || v.Contains('\n'))
            {
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }
            return v;
        }
    }
}
