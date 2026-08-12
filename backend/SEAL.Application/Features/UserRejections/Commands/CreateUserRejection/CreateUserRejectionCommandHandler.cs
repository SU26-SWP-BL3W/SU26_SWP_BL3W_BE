using MediatR;
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

        public CreateUserRejectionCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateUserRejectionResponseModel>> Handle(CreateUserRejectionCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra User bị từ chối có tồn tại không
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Model.UserId);
            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{request.Model.UserId}' không tồn tại.");
            }

            // 2. Kiểm tra người thực hiện từ chối có tồn tại và phải là Admin không
            var adminUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Model.RejectedBy);
            if (adminUser == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người thực hiện từ chối có ID '{request.Model.RejectedBy}' không tồn tại.");
            }

            if (!adminUser.IsAdmin)
            {
                return BaseException.BadRequestInvaildInputResponse($"Người thực hiện từ chối phải là Admin.");
            }

            // 3. Tạo bản ghi UserRejection
            var userRejection = new UserRejection
            {
                UserId = request.Model.UserId,
                RejectedBy = request.Model.RejectedBy,
                Reason = request.Model.Reason
            };

            // 4. Cập nhật trạng thái User thành không được duyệt
            user.IsApproved = false;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);

            // 5. Lưu vào Database
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
