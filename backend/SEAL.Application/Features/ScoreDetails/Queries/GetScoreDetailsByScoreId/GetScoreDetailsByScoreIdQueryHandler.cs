using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.ScoreDetails.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.ScoreDetails.Queries.GetScoreDetailsByScoreId
{
    public class GetScoreDetailsByScoreIdQueryHandler : IRequestHandler<GetScoreDetailsByScoreIdQuery, Result<PagedResult<ScoreDetailModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetScoreDetailsByScoreIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<ScoreDetailModel>>> Handle(GetScoreDetailsByScoreIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<ScoreDetail>().Entities
                .Where(d => d.ScoreId == request.ScoreId);

            var pagedDetails = await query.ToPagedResultAsync(
                request,
                d => new ScoreDetailModel
                {
                    Id = d.Id,
                    ScoreId = d.ScoreId,
                    TemplateId = d.TemplateId,
                    CriteriaId = d.CriteriaId,
                    Value = d.Value,
                    CreatedTime = d.CreatedTime,
                    LastUpdatedTime = d.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedDetails;
        }
    }
}



