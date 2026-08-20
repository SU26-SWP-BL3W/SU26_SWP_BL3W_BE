using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown
{
    public class GetTeamScoreBreakdownQueryHandler : IRequestHandler<GetTeamScoreBreakdownQuery, Result<TeamScoreBreakdownModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetTeamScoreBreakdownQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<TeamScoreBreakdownModel>> Handle(GetTeamScoreBreakdownQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Nhóm có ID '{request.TeamId}' không tồn tại.");
            }

            // Quyền: thành viên (TeamLeader/TeamMember) của đội, EventCoordinator, Admin,
            // hoặc MENTOR đang cố vấn hạng mục mà đội có bài nộp (xem chi tiết ở bước dưới).
            //
            // LƯU Ý CỐ Ý KHÔNG cho GIÁM KHẢO (Judge) vào đây: endpoint này trả điểm của TẤT CẢ giám
            // khảo, cho Judge xem sẽ lộ điểm của đồng nghiệp -> dễ gây thiên vị khi chấm. Judge xem
            // phiếu CỦA CHÍNH MÌNH qua GET /api/Scores/event-role/{eventRoleId}.
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isMember = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId && er.TeamId == request.TeamId
                   && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                cancellationToken);
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            // Mentor: cố vấn cần biết đội mình tư vấn được chấm ra sao. Chỉ tính vai trò Mentor CÒN
            // HIỆU LỰC trong đúng sự kiện của đội, và giới hạn theo phạm vi được phân công:
            //   - Mentor gắn Track  -> chỉ xem đội CÓ bài nộp trong hạng mục mình phụ trách
            //     (không lộ đội của hạng mục khác).
            //   - Mentor cấp sự kiện (TrackId null) -> xem được mọi đội trong sự kiện đó.
            bool isMentor = false;
            if (!isAdmin && !isMember && !isCoordinator)
            {
                var nowUtc = System.DateTime.UtcNow;
                var mentorTrackIds = await _unitOfWork.GetRepository<EventRole>().Entities
                    .AsNoTracking()
                    .Include(er => er.Event)
                    .Where(er => er.UserId == currentUserId
                              && er.EventId == team.EventId
                              && er.RoleName == EventRoleType.Mentor
                              && (er.ExpiredAt ?? er.Event.EndDate) > nowUtc)
                    .Select(er => er.TrackId)
                    .ToListAsync(cancellationToken);

                if (mentorTrackIds.Count > 0)
                {
                    if (mentorTrackIds.Any(t => string.IsNullOrEmpty(t)))
                    {
                        isMentor = true;   // Mentor cấp sự kiện
                    }
                    else
                    {
                        var scopedTrackIds = mentorTrackIds.Where(t => !string.IsNullOrEmpty(t)).ToList();
                        isMentor = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                            s => s.TeamId == request.TeamId && scopedTrackIds.Contains(s.TrackId),
                            cancellationToken);
                    }
                }
            }

            if (!isAdmin && !isMember && !isCoordinator && !isMentor)
            {
                return new BaseException.ForbiddenException("Bạn chỉ có thể xem điểm của đội mình.");
            }

            var result = new TeamScoreBreakdownModel { TeamId = team.Id, TeamName = team.Name };

            // 1. Các bài nộp của đội (kèm Track + Round).
            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .AsNoTracking()
                .Include(s => s.Track)
                .Include(s => s.Round)
                .Where(s => s.TeamId == request.TeamId)
                .ToListAsync(cancellationToken);
            if (submissions.Count == 0)
            {
                return result;
            }

            var submitIds = submissions.Select(s => s.Id).ToList();
            var templateIds = submissions
                .Where(s => s.Track != null && s.Track.TemplateId != null)
                .Select(s => s.Track!.TemplateId!)
                .Distinct().ToList();
            var roundIds = submissions
                .Select(s => s.RoundId)
                .Distinct().ToList();

            // 2. Phiếu chấm của các bài đó (kèm giám khảo + điểm chi tiết).
            var scores = await _unitOfWork.GetRepository<Score>().Entities
                .AsNoTracking()
                .Include(s => s.EventRole)!.ThenInclude(er => er.User)
                .Include(s => s.ScoreDetails)
                .Where(s => submitIds.Contains(s.SubmitResultId))
                .ToListAsync(cancellationToken);

            // 3. Cấu hình tiêu chí (MaxScore/Weight) + tên tiêu chí để hiển thị.
            var templateCriterias = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .AsNoTracking()
                .Where(tc => templateIds.Contains(tc.TemplateId))
                .ToListAsync(cancellationToken);
            var tcMap = templateCriterias.ToDictionary(tc => tc.TemplateId + "|" + tc.CriteriaId);

            var criteriaIds = templateCriterias.Select(tc => tc.CriteriaId).Distinct().ToList();
            var criteriaNames = await _unitOfWork.GetRepository<Criteria>().Entities
                .AsNoTracking()
                .Where(c => criteriaIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.CriteriaName, cancellationToken);

            // 4. Vòng nào đã công bố kết quả (có FinalResult).
            var publishedRoundIds = await _unitOfWork.GetRepository<FinalResult>().Entities
                .AsNoTracking()
                .Where(fr => roundIds.Contains(fr.RoundId))
                .Select(fr => fr.RoundId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var publishedSet = publishedRoundIds.ToHashSet();

            // 5. Gom theo bài nộp -> giám khảo -> tiêu chí.
            foreach (var sub in submissions)
            {
                var templateId = sub.Track?.TemplateId;
                var subModel = new SubmissionScoreBreakdown
                {
                    SubmitResultId = sub.Id,
                    TrackName = sub.Track?.TrackName ?? string.Empty,
                    RoundId = sub.RoundId,
                    RoundName = sub.Round?.RoundName ?? string.Empty,
                    RoundPublished = publishedSet.Contains(sub.RoundId),
                };

                foreach (var score in scores.Where(s => s.SubmitResultId == sub.Id))
                {
                    var judgeModel = new JudgeScoreBreakdown
                    {
                        JudgeName = score.EventRole?.User?.FullName ?? "Giám khảo",
                        TotalScore = score.TotalScore,
                        Comment = score.Comment,
                        IsSubmitted = score.IsSubmitted,
                    };

                    foreach (var d in score.ScoreDetails)
                    {
                        tcMap.TryGetValue(d.TemplateId + "|" + d.CriteriaId, out var tc);
                        judgeModel.Criteria.Add(new CriterionScoreLine
                        {
                            CriteriaName = criteriaNames.TryGetValue(d.CriteriaId, out var name) ? name : "Tiêu chí",
                            Value = d.Value,
                            MaxScore = tc?.MaxScore ?? 0m,
                            Weight = tc?.Weight ?? 0m,
                        });
                    }

                    subModel.JudgeScores.Add(judgeModel);
                }

                result.Submissions.Add(subModel);
            }

            return result;
        }
    }
}


