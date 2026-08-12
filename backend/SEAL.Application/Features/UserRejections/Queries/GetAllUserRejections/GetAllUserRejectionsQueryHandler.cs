using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections.Queries.GetAllUserRejections
{
    public class GetAllUserRejectionsQueryHandler : IRequestHandler<GetAllUserRejectionsQuery, Result<PagedResult<UserRejectionModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllUserRejectionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<UserRejectionModel>>> Handle(GetAllUserRejectionsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<UserRejection>().GetQueryable();

            // 1. Áp dụng filter
            if (request.FromDate.HasValue)
                query = query.Where(ur => ur.CreatedTime >= request.FromDate.Value);
            
            if (request.ToDate.HasValue)
                query = query.Where(ur => ur.CreatedTime <= request.ToDate.Value);
            
            if (!string.IsNullOrEmpty(request.RejectedBy))
                query = query.Where(ur => ur.RejectedBy == request.RejectedBy);
            
            if (!string.IsNullOrEmpty(request.UserId))
                query = query.Where(ur => ur.UserId == request.UserId);

            // 2. Sử dụng Extension để phân trang song song và dynamic sorting
            return await query.ToPagedResultAsync(
                request: request,
                selector: ur => new UserRejectionModel
                {
                    Id = ur.Id,
                    UserId = ur.UserId,
                    RejectedBy = ur.RejectedBy,
                    Reason = ur.Reason,
                    IsActive = ur.IsActive,
                    CreatedTime = ur.CreatedTime
                },
                cancellationToken: cancellationToken
            );
        }
    }
}
