// [FLOW3-DOITHI][RejectTeamRegistration] EC tu choi ho so dang ky doi kem ly do, tra doi ve trang thai duoc sua lai.

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

namespace SEAL_Application.Features.Teams.Commands.RejectTeamRegistration
{
    /// <summary>
    /// EC/Admin từ chối duyệt đội: PendingApproval -> Forming (mở khóa để đội thay người
    /// rồi chốt lại). Gửi email kèm LÝ DO cho trưởng nhóm.
    /// </summary>
    public class RejectTeamRegistrationCommandHandler : IRequestHandler<RejectTeamRegistrationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notifications;
        private readonly ILogger<RejectTeamRegistrationCommandHandler> _logger;

        public RejectTeamRegistrationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IEmailService emailService,
            INotificationService notifications,
            ILogger<RejectTeamRegistrationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _emailService = emailService;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RejectTeamRegistrationCommand request, CancellationToken cancellationToken)
        {
            // 0. Bắt buộc đăng nhập
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Đội tồn tại
            var team = await _unitOfWork.GetRepository<Team>().Entities
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Đội có ID '{request.TeamId}' không tồn tại.");
            }

            // 2. Quyền: chỉ Admin hoặc EventCoordinator của ĐÚNG sự kiện đó
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên (EC) của sự kiện được từ chối đội.");
            }

            // 3. Đội phải đang CHỜ DUYỆT
            if (team.Status != TeamStatus.PendingApproval)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    $"Chỉ từ chối được đội đang chờ duyệt (hiện tại: {team.Status}).");
            }

            // 4. Trả về Forming để đội tự thay người và chốt lại (theo khuyến nghị trong yêu cầu)
            team.Status = TeamStatus.Forming;
            // Lưu lý do vào DB để FE đọc lại được — email có thể không ai mở.
            team.LastRejectReason = request.Reason;
            team.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<Team>().UpdateAsync(team);

            var leaderIds = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == team.Id && er.RoleName == EventRoleType.TeamLeader)
                .Select(er => er.UserId)
                .ToListAsync(cancellationToken);
            await _notifications.NotifyManyAsync(
                leaderIds,
                "Đội bị từ chối duyệt",
                $"Đội {team.Name} bị từ chối: {request.Reason}",
                "danger",
                "/my-team",
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Báo TRƯỞNG NHÓM kèm lý do (lỗi SMTP không chặn nghiệp vụ từ chối)
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
                    heading: "Đăng ký đội bị từ chối duyệt",
                    greetingName: leader.FullName,
                    introHtml: $"Đăng ký của đội <b>{team.Name}</b> trong <b>{eventName}</b> đã bị <b>TỪ CHỐI</b>.",
                    calloutLabel: "Lý do",
                    calloutHtml: reasonHtml,
                    calloutKind: EmailTemplate.Callout.Danger,
                    showLoginHint: false,
                    noteHtml: "Đội đã được mở khóa — vui lòng cập nhật thành viên rồi chốt danh sách lại.");
                try { await _emailService.SendEmailAsync(leader.Email, $"[SEAL] Đội {team.Name} bị từ chối duyệt", body); }
                catch (Exception ex) { _logger.LogWarning(ex, "Gửi email từ chối đội thất bại cho {Email}", leader.Email); }
            }

            return true;
        }
    }
}


