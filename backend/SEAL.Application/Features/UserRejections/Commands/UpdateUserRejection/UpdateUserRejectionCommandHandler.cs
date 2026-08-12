using MediatR;
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

        public UpdateUserRejectionCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateUserRejectionResponseModel>> Handle(UpdateUserRejectionCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm bản ghi từ chối
            var userRejection = await _unitOfWork.GetRepository<UserRejection>().GetByIdAsync(request.Id);
            if (userRejection == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bản ghi từ chối có ID '{request.Id}' không tồn tại.");
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
