using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Rounds.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Rounds.Queries.GetRoundsByEventId
{
    public class GetRoundsByEventIdQueryHandler : IRequestHandler<GetRoundsByEventIdQuery, Result<PagedResult<RoundModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRoundsByEventIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<RoundModel>>> Handle(GetRoundsByEventIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Round>().Entities
                .Where(r => r.EventId == request.EventId);

            var pagedRounds = await query.ToPagedResultAsync(
                request,
                r => new RoundModel
                {
                    Id = r.Id,
                    EventId = r.EventId,
                    RoundName = r.RoundName,
                    RoundNumber = r.RoundNumber,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    AdvancementRule = r.AdvancementRule,
                    ScoringStartDate = r.ScoringStartDate,
                    ScoringEndDate = r.ScoringEndDate,
                    CreatedTime = r.CreatedTime,
                    LastUpdatedTime = r.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedRounds;
        }
    }
}



