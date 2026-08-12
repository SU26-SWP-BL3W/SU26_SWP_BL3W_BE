using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.ScoreDetails.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.ScoreDetails.Queries.GetScoreDetailById
{
    public class GetScoreDetailByIdQueryHandler : IRequestHandler<GetScoreDetailByIdQuery, Result<ScoreDetailModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetScoreDetailByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ScoreDetailModel?>> Handle(GetScoreDetailByIdQuery request, CancellationToken cancellationToken)
        {
            var scoreDetail = await _unitOfWork.GetRepository<ScoreDetail>().GetByIdAsync(request.Id);
            if (scoreDetail == null)
            {
                return null;
            }

            return new ScoreDetailModel
            {
                Id = scoreDetail.Id,
                ScoreId = scoreDetail.ScoreId,
                TemplateId = scoreDetail.TemplateId,
                CriteriaId = scoreDetail.CriteriaId,
                Value = scoreDetail.Value,
                CreatedTime = scoreDetail.CreatedTime,
                LastUpdatedTime = scoreDetail.LastUpdatedTime
            };
        }
    }
}

