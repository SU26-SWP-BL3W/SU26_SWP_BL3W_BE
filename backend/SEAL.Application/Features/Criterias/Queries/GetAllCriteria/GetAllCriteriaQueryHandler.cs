using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Criterias.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias.Queries.GetAllCriteria
{
    public class GetAllCriteriaQueryHandler : IRequestHandler<GetAllCriteriaQuery, Result<PagedResult<CriteriaModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllCriteriaQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<CriteriaModel>>> Handle(GetAllCriteriaQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Criteria>().Entities;

            return await query.ToPagedResultAsync(
                request,
                c => new CriteriaModel
                {
                    Id = c.Id,
                    CriteriaName = c.CriteriaName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedTime = c.CreatedTime,
                    LastUpdatedTime = c.LastUpdatedTime
                },
                cancellationToken);
        }
    }
}



