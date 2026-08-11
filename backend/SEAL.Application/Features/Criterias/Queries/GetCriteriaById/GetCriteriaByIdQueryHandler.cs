using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Criterias.Models;
using SEAL_Domain.Base;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias.Queries.GetCriteriaById
{
    public class GetCriteriaByIdQueryHandler : IRequestHandler<GetCriteriaByIdQuery, Result<CriteriaModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCriteriaByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CriteriaModel>> Handle(GetCriteriaByIdQuery request, CancellationToken cancellationToken)
        {
            var criteria = await _unitOfWork.GetRepository<Criteria>().GetByIdAsync(request.Id);

            if (criteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            return new CriteriaModel
            {
                Id = criteria.Id,
                CriteriaName = criteria.CriteriaName,
                Description = criteria.Description,
                IsActive = criteria.IsActive,
                CreatedTime = criteria.CreatedTime,
                LastUpdatedTime = criteria.LastUpdatedTime
            };
        }
    }
}

