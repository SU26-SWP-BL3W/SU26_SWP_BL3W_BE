using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.DeleteUser
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public DeleteUserCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Id);
            if (user == null)
            {
                throw new Exception("User không tồn tại.");
            }

            // Kiểm tra Quyền hạn (Chỉ Admin)
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new Exception("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                throw new SEAL_Domain.Base.BaseException.ForbiddenException("Bạn không có quyền xóa người dùng này.");
            }

            // Không cho admin tự xoá tài khoản của chính mình (tránh tự khoá khỏi hệ thống).
            // Vì người gọi luôn là admin, chặn tự xoá cũng bảo đảm không bao giờ xoá mất admin cuối cùng.
            if (user.Id == currentUserId)
            {
                throw SEAL_Domain.Base.BaseException.BadRequestInvaildInputResponse(
                    "Bạn không thể tự xoá tài khoản của chính mình.");
            }

            await _unitOfWork.GetRepository<User>().DeleteAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

