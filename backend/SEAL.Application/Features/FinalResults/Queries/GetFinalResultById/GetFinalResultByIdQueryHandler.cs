using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.FinalResults.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Queries.GetFinalResultById
{
    public class GetFinalResultByIdQueryHandler : IRequestHandler<GetFinalResultByIdQuery, Result<FinalResultModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetFinalResultByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<FinalResultModel?>> Handle(GetFinalResultByIdQuery request, CancellationToken cancellationToken)
        {
            var finalResult = await _unitOfWork.GetRepository<FinalResult>().GetByIdAsync(request.Id);
            if (finalResult == null)
            {
                return null;
            }

            return new FinalResultModel
            {
                Id = finalResult.Id,
                TeamId = finalResult.TeamId,
                RoundId = finalResult.RoundId,
                EventId = finalResult.EventId,
                TrackId = finalResult.TrackId,
                PrizeId = finalResult.PrizeId,
                FinalScore = finalResult.FinalScore,
                Rank = finalResult.Rank,
                IsAdvanced = finalResult.IsAdvanced,
                IsPublished = finalResult.IsPublished,
                CreatedTime = finalResult.CreatedTime,
                LastUpdatedTime = finalResult.LastUpdatedTime
            };
        }
    }
}

