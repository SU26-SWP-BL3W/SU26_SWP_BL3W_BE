using MediatR;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.UserRejections.Commands.CreateUserRejection.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections.Commands.CreateUserRejection
{
    public class CreateUserRejectionCommandHandler : IRequestHandler<CreateUserRejectionCommand, Result<CreateUserRejectionResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateUserRejectionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateUserRejectionResponseModel>> Handle(CreateUserRejectionCommand request, CancellationToken cancellationToken)
        {
            // 0. Người thực hiện phải xác thực được và phải là Admin — lấy từ token, KHÔNG tin
            //    field RejectedBy do client tự truyền (trước đây cho phép giả mạo bất kỳ admin nào).
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var adminUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (adminUser == null || !adminUser.IsAdmin)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hệ thống mới có quyền tạo bản ghi từ chối.");
            }

            // 1. Kiểm tra User bị từ chối có tồn tại không
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Model.UserId);
            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{request.Model.UserId}' không tồn tại.");
            }

            // 2. Tạo bản ghi UserRejection — RejectedBy lấy từ currentUserId đã xác thực ở trên
            var userRejection = new UserRejection
            {
                UserId = request.Model.UserId,
                RejectedBy = currentUserId,
                Reason = request.Model.Reason
            };

            // 3. Cập nhật trạng thái User thành không được duyệt
            user.IsApproved = false;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);

            // 4. Lưu vào Database
            await _unitOfWork.GetRepository<UserRejection>().AddAsync(userRejection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateUserRejectionResponseModel
            {
                Id = userRejection.Id,
                UserId = userRejection.UserId,
                RejectedBy = userRejection.RejectedBy,
                Reason = userRejection.Reason,
                CreatedTime = userRejection.CreatedTime
            };
        }
    }
}

