using MediatR;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection
{
    public class UpdateUserRejectionCommandHandler : IRequestHandler<UpdateUserRejectionCommand, Result<UpdateUserRejectionResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateUserRejectionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UpdateUserRejectionResponseModel>> Handle(UpdateUserRejectionCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm bản ghi từ chối
            var userRejection = await _unitOfWork.GetRepository<UserRejection>().GetByIdAsync(request.Id);
            if (userRejection == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bản ghi từ chối có ID '{request.Id}' không tồn tại.");
            }

            // 1b. Kiểm tra Ownership / Quyền hạn — chỉ Admin hoặc người đã tạo bản ghi mới được sửa
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
                return new BaseException.ForbiddenException("Bạn không có quyền sửa bản ghi từ chối này.");
            }

            // 2. Cập nhật thông tin
            userRejection.Reason = request.Model.Reason;
            userRejection.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 3. Lưu vào Database
            await _unitOfWork.GetRepository<UserRejection>().UpdateAsync(userRejection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateUserRejectionResponseModel
            {
                Id = userRejection.Id,
                UserId = userRejection.UserId,
                RejectedBy = userRejection.RejectedBy,
                Reason = userRejection.Reason,
                LastUpdatedTime = userRejection.LastUpdatedTime
            };
        }
    }
}

