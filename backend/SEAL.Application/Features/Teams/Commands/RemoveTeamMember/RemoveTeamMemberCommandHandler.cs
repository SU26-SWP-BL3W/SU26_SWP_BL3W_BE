using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.RemoveTeamMember
{
    public class RemoveTeamMemberCommandHandler : IRequestHandler<RemoveTeamMemberCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public RemoveTeamMemberCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
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

            // 2. Tìm EventRole của thành viên trong team này
            var memberRole = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstOrDefaultAsync(er => er.UserId == request.UserId
                                        && er.TeamId == request.TeamId,
                                     cancellationToken);
            if (memberRole == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng '{request.UserId}' không phải thành viên của nhóm này.");
            }

            // 3. KHÔNG cho phép remove TeamLeader (Leader phải Delete Team)
            if (memberRole.RoleName == EventRoleType.TeamLeader)
            {
                return BaseException.BadRequestInvaildInputResponse("Không thể xóa TeamLeader khỏi nhóm. Hãy xóa nhóm hoặc chuyển vai trò Leader trước.");
            }

            // 4. Kiểm tra quyền: caller phải là TeamLeader của team này, hoặc EventCoordinator, hoặc Admin
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isLeaderOfThisTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.TeamId == request.TeamId
                   && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                team.EventId,
                new[] { EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isLeaderOfThisTeam && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa thành viên khỏi nhóm này. Chỉ TeamLeader hoặc EventCoordinator được phép.");
            }

            // 4b. Đội đã đăng ký chính thức (Registered) thì bị KHÓA — không xóa thành viên trực tiếp được.
            //     (Nhất quán với AddTeamMember và LeaveTeam; muốn đổi roster phải đưa đội về Forming.)
            if (team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã đăng ký chính thức (đã khóa) nên không thể xóa thành viên.");
            }

            // 5. Xóa cứng EventRole của thành viên đó
            await _unitOfWork.GetRepository<EventRole>().DeleteAsync(memberRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


