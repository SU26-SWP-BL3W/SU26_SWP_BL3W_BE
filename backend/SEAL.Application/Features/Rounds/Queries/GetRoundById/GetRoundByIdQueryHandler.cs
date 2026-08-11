using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Rounds.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Rounds.Queries.GetRoundById
{
    public class GetRoundByIdQueryHandler : IRequestHandler<GetRoundByIdQuery, Result<RoundModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRoundByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RoundModel?>> Handle(GetRoundByIdQuery request, CancellationToken cancellationToken)
        {
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.Id);
            if (round == null)
            {
                return null;
            }

            return new RoundModel
            {
                Id = round.Id,
                EventId = round.EventId,
                RoundName = round.RoundName,
                RoundNumber = round.RoundNumber,
                StartDate = round.StartDate,
                EndDate = round.EndDate,
                AdvancementRule = round.AdvancementRule,
                ScoringStartDate = round.ScoringStartDate,
                ScoringEndDate = round.ScoringEndDate,
                CreatedTime = round.CreatedTime,
                LastUpdatedTime = round.LastUpdatedTime
            };
        }
    }
}

