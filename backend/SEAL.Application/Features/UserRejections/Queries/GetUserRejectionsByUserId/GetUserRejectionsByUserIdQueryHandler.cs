using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId
{
    public class GetUserRejectionsByUserIdQueryHandler : IRequestHandler<GetUserRejectionsByUserIdQuery, Result<List<UserRejectionModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserRejectionsByUserIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<UserRejectionModel>>> Handle(GetUserRejectionsByUserIdQuery request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra xem User có tồn tại không
            var userExist = await _unitOfWork.GetRepository<User>().AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExist)
            {
                return BaseException.BadRequestInvaildInputResponse($"Người dùng có ID '{request.UserId}' không tồn tại trong hệ thống.");
            }

            // 2. Lấy lịch sử từ chối của User
            var rejections = await _unitOfWork.GetRepository<UserRejection>().FindAsync(ur => ur.UserId == request.UserId);

            // 3. Map sang Response Model
            return rejections.Select(ur => new UserRejectionModel
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RejectedBy = ur.RejectedBy,
                Reason = ur.Reason,
                IsActive = ur.IsActive,
                CreatedTime = ur.CreatedTime
            }).ToList();
        }
    }
}
