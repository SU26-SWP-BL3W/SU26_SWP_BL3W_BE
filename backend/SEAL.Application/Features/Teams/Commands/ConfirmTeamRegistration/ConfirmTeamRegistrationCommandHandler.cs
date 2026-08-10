// [FLOW3-DOITHI][ConfirmTeamRegistration] Chot danh sach doi (yeu cau 3-5 thanh vien) de gui EC duyet, khoa khong cho sua danh sach nua.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration
{
    public class ConfirmTeamRegistrationCommandHandler : IRequestHandler<ConfirmTeamRegistrationCommand, Result<ConfirmTeamRegistrationResponseModel>>
    {
        // Đội phải có 3-5 thành viên để đăng ký chính thức
        private const int MIN_TEAM_SIZE = 3;
        private const int MAX_TEAM_SIZE = 5;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public ConfirmTeamRegistrationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<ConfirmTeamRegistrationResponseModel>> Handle(ConfirmTeamRegistrationCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Team tồn tại
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Nhóm có ID '{request.TeamId}' không tồn tại.");
            }

            // 2. Đội phải đang Forming (chưa đăng ký)
            if (team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse($"Đội không ở trạng thái cho phép đăng ký (hiện tại: {team.Status}).");
            }

            // 3. Quyền: caller là TeamLeader của đội (hoặc Coordinator/Admin)
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isLeaderOfThisTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.TeamId == request.TeamId
                   && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            if (!isAdmin && !isLeaderOfThisTeam && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ TeamLeader của đội hoặc EventCoordinator được xác nhận đăng ký.");
            }

            // 4. Final validation — lấy danh sách thành viên của đội
            var memberRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == request.TeamId
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .ToListAsync(cancellationToken);

            var memberCount = memberRoles.Count;

            // 4a. Đủ 3-5 thành viên
            if (memberCount < MIN_TEAM_SIZE || memberCount > MAX_TEAM_SIZE)
            {
                return BaseException.BadRequestInvaildInputResponse($"Đội cần {MIN_TEAM_SIZE}-{MAX_TEAM_SIZE} thành viên để đăng ký (hiện tại: {memberCount}).");
            }

            // 4b. (ĐÃ BỎ) Trước đây yêu cầu mọi thành viên phải được duyệt TÀI KHOẢN (IsApproved).
            //     Nay việc xét duyệt chuyển sang cấp ĐỘI THI: đội chốt danh sách -> PendingApproval
            //     -> EC/Admin duyệt cả đội (ApproveTeamRegistration). Không chặn theo IsApproved nữa.
            var memberUserIds = memberRoles.Select(er => er.UserId).ToList();

            // 4b'. Tất cả thành viên đã NỘP HỒ SƠ THÍ SINH — bất biến "đã nộp hồ sơ" của hệ thống:
            //      IsStudent (chỉ UpdateStudentProfile set) + SchoolId (luôn bắt buộc khi nộp hồ sơ).
            //      Hồ sơ được phép bổ sung trong lúc Forming; CHỐT đăng ký thì phải đủ.
            var profileCount = await _unitOfWork.GetRepository<User>().Entities
                .CountAsync(u => memberUserIds.Contains(u.Id) && u.IsStudent && u.SchoolId != null, cancellationToken);
            if (profileCount != memberCount)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Có thành viên chưa nộp hồ sơ thí sinh. Vui lòng hoàn tất hồ sơ trước khi chốt đăng ký đội.");
            }

            // 4c. Còn trong HẠN ĐĂNG KÝ của sự kiện (nhất quán với CreateTeam/InviteTeamMember).
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(team.EventId);
            if (eventEntity != null)
            {
                var nowUtc = System.DateTime.UtcNow;
                if (eventEntity.RegistrationStartDate.HasValue && nowUtc < eventEntity.RegistrationStartDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Chưa đến thời gian đăng ký thi, không thể chốt đăng ký đội.");
                }
                if (eventEntity.RegistrationEndDate.HasValue && nowUtc > eventEntity.RegistrationEndDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian đăng ký thi, không thể chốt đăng ký đội.");
                }
            }

            // 5. Khóa danh sách đội — chuyển sang CHỜ EC/Admin DUYỆT (không còn tự động Registered).
            //    Đội chỉ chính thức được thi sau khi EC duyệt (ApproveTeamRegistration).
            team.Status = TeamStatus.PendingApproval;
            // Dọn lý do từ chối cũ — đội đã sửa và nộp lại, giữ lại sẽ hiện mãi một lý do đã xử lý xong.
            team.LastRejectReason = null;
            team.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<Team>().UpdateAsync(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // TODO: Thông báo cho tất cả thành viên + EventCoordinator

            return new ConfirmTeamRegistrationResponseModel
            {
                TeamId = team.Id,
                Status = team.Status.ToString(),
                MemberCount = memberCount
            };
        }
    }
}


