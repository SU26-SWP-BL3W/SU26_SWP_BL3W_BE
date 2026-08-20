// [FLOW3-DOITHI][DisqualifyTeam] EC loai doi vi pham; ly do luu LastRejectReason (phan biet bang Status=Disqualified).

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.DisqualifyTeam
{
    public class DisqualifyTeamCommandHandler : IRequestHandler<DisqualifyTeamCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<DisqualifyTeamCommandHandler> _logger;

        public DisqualifyTeamCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IEmailService emailService,
            IAuditLogService auditLogService,
            ILogger<DisqualifyTeamCommandHandler> logger)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        public async Task<Result<bool>> Handle(DisqualifyTeamCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var team = await _unitOfWork.GetRepository<Team>().Entities
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Đội có ID '{request.TeamId}' không tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên (EC) của sự kiện được loại đội.");
            }

            if (team.Status != TeamStatus.Registered)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    $"Chỉ loại được đội đang thi (Registered). Hiện tại: {team.Status}.");
            }

            team.Status = TeamStatus.Disqualified;
            team.IsActive = false;
            team.LastRejectReason = request.Reason;
            team.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<Team>().UpdateAsync(team);

            var allResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(fr => fr.TeamId == team.Id)
                .ToListAsync(cancellationToken);
            if (allResults.Count > 0)
            {
                // Xóa cả bản đã công bố — đội bị loại không được còn trên BXH.
                await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(allResults);
            }

            // Vô hiệu (không xóa) bài nộp của đội bị loại — giữ lại để đối chiếu/kiểm tra sau này,
            // nhưng không còn tính là bài nộp đang hoạt động (đúng ý nghĩa cờ IsActive đã có sẵn).
            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .Where(sr => sr.TeamId == team.Id && sr.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var submission in submissions)
            {
                submission.IsActive = false;
                await _unitOfWork.GetRepository<SubmitResult>().UpdateAsync(submission);
            }

            await _auditLogService.AppendAsync(
                AuditActions.DisqualifyTeam,
                AuditEntityTypes.Team,
                team.Id,
                team.EventId,
                $"Loại đội {team.Name}",
                new { reason = request.Reason },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var leaders = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == team.Id && er.RoleName == EventRoleType.TeamLeader)
                .Select(er => er.User)
                .ToListAsync(cancellationToken);
            var eventName = team.Event?.EventName ?? "sự kiện";
            var reasonHtml = WebUtility.HtmlEncode(request.Reason).Replace("\n", "<br>");
            foreach (var leader in leaders)
            {
                if (leader == null || string.IsNullOrEmpty(leader.Email)) continue;
                var body = EmailTemplate.Render(
                    heading: "Đội thi đã bị loại",
                    greetingName: leader.FullName,
                    introHtml: $"Đội <b>{team.Name}</b> trong <b>{eventName}</b> đã bị <b>LOẠI</b> khỏi cuộc thi.",
                    calloutLabel: "Lý do",
                    calloutHtml: reasonHtml,
                    calloutKind: EmailTemplate.Callout.Danger,
                    showLoginHint: false);
                try { await _emailService.SendEmailAsync(leader.Email, $"[SEAL] Đội {team.Name} bị loại", body); }
                catch (Exception ex) { _logger.LogWarning(ex, "Gửi email loại đội thất bại cho {Email}", leader.Email); }
            }

            return true;
        }
    }
}
