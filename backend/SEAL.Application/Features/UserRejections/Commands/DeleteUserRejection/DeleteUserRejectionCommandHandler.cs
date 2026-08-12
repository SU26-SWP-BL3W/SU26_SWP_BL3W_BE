using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections.Commands.DeleteUserRejection
{
    public class DeleteUserRejectionCommandHandler : IRequestHandler<DeleteUserRejectionCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public DeleteUserRejectionCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(DeleteUserRejectionCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm bản ghi từ chối
            var userRejection = await _unitOfWork.GetRepository<UserRejection>().GetByIdAsync(request.Id);
            if (userRejection == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bản ghi từ chối có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isOwner = userRejection.CreatedBy == currentUserId;

            if (!isAdmin && !isOwner)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa bản ghi từ chối này.");
            }

            var userId = userRejection.UserId;

            // 2. Xóa bản ghi từ chối
            await _unitOfWork.GetRepository<UserRejection>().DeleteAsync(userRejection);

            // 3. Kiểm tra xem đây có phải là bản ghi từ chối cuối cùng đang hoạt động không
            var hasOtherRejections = await _unitOfWork.GetRepository<UserRejection>()
                .AnyAsync(ur => ur.UserId == userId && ur.Id != request.Id && ur.IsActive, cancellationToken);

            if (!hasOtherRejections)
            {
                // Khôi phục trạng thái của User về chưa duyệt (IsApproved = false)
                var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
                if (user != null)
                {
                    user.IsApproved = false;
                    await _unitOfWork.GetRepository<User>().UpdateAsync(user);
                }
            }

            // 4. Lưu thay đổi
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
