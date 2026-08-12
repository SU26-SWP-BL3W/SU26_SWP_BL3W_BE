// [FLOW3-DOITHI][ApproveTeamRegistration] EC duyet doi da chot danh sach, chuyen trang thai doi sang Registered.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.ApproveTeamRegistration
{
    /// <summary>
    /// EC/Admin duyệt đội đã chốt danh sách: PendingApproval -> Registered.
    /// Gửi email báo cho toàn bộ thành viên đội là đã được duyệt.
    /// </summary>
    public class ApproveTeamRegistrationCommandHandler : IRequestHandler<ApproveTeamRegistrationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IEmailService _emailService;

        public ApproveTeamRegistrationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _emailService = emailService;
        }

        public async Task<Result<bool>> Handle(ApproveTeamRegistrationCommand request, CancellationToken cancellationToken)
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
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên (EC) của sự kiện được duyệt đội.");
            }

            // 3. Đội phải đang CHỜ DUYỆT
            if (team.Status != TeamStatus.PendingApproval)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    $"Chỉ duyệt được đội đang chờ duyệt (hiện tại: {team.Status}).");
            }

            // 4. Duyệt -> chính thức được thi
            team.Status = TeamStatus.Registered;
            team.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<Team>().UpdateAsync(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Báo cho toàn đội (lỗi SMTP không chặn nghiệp vụ duyệt)
            var members = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == team.Id
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .Select(er => er.User)
                .ToListAsync(cancellationToken);
            var eventName = team.Event?.EventName ?? "sự kiện";
            foreach (var m in members)
            {
                if (m == null || string.IsNullOrEmpty(m.Email)) continue;
                var body =
                    $"<h3>Chào {m.FullName},</h3>" +
                    $"<p>Đội <b>{team.Name}</b> của bạn đã được <b>DUYỆT</b> tham gia <b>{eventName}</b>.</p>" +
                    $"<p>Đội đã chính thức đủ điều kiện thi đấu và nộp bài.</p>";
                try { await _emailService.SendEmailAsync(m.Email, $"[SEAL] Đội {team.Name} đã được duyệt", body); }
                catch { /* Bỏ qua lỗi SMTP */ }
            }

            return true;
        }
    }
}


