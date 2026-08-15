using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.CancelTeamInvitation
{
    public class CancelTeamInvitationCommandHandler : IRequestHandler<CancelTeamInvitationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CancelTeamInvitationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(CancelTeamInvitationCommand request, CancellationToken cancellationToken)
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

            // 2. Chỉ Đội trưởng của team (hoặc Admin/EC) mới có quyền hủy lời mời
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isLeaderOfThisTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.TeamId == request.TeamId
                   && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            bool isEC = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.EventId == team.EventId
                   && er.RoleName == EventRoleType.EventCoordinator,
                cancellationToken);

            if (!isLeaderOfThisTeam && !isAdmin && !isEC)
            {
                return new BaseException.ForbiddenException("Chỉ Đội trưởng của nhóm mới có quyền hủy lời mời gia nhập đội.");
            }

            // 3. Tìm lời mời
            var invitation = await _unitOfWork.GetRepository<TeamInvitation>().GetByIdAsync(request.InvitationId);
            if (invitation == null || invitation.TeamId != request.TeamId)
            {
                return BaseException.BadRequestNotFoundResponse($"Lời mời '{request.InvitationId}' không tồn tại trong nhóm này.");
            }

            // 4. Lời mời phải ở trạng thái đang chờ mới có thể hủy
            if (invitation.Status != TeamInvitationStatus.PendingAccept && invitation.Status != TeamInvitationStatus.TransferPending)
            {
                return BaseException.BadRequestResponse($"Không thể hủy lời mời đã ở trạng thái '{invitation.Status}'.");
            }

            // 5. Cập nhật trạng thái sang Cancelled
            invitation.Status = TeamInvitationStatus.Cancelled;
            _unitOfWork.GetRepository<TeamInvitation>().Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
