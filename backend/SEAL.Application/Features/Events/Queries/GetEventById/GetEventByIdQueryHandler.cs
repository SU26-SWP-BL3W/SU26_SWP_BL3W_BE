using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Events.Models;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SEAL_Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEventByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<EventModel?>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"ID cần tìm: '{request.Id}'");
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetQueryable()
                .Include(e => e.Rounds)
                    .ThenInclude(r => r.Tracks)
                .Include(e => e.Prizes)
                .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

            if (eventEntity == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy sự kiện");
            }

            return new EventModel
            {
                Id = eventEntity.Id,
                EventName = eventEntity.EventName,
                Season = eventEntity.Season,
                Year = eventEntity.Year,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                RegistrationStartDate = eventEntity.RegistrationStartDate,
                RegistrationEndDate = eventEntity.RegistrationEndDate,
                Description = eventEntity.Description,
                Status = eventEntity.Status,
                PhotoEventUrl = eventEntity.PhotoEventUrl,
                MaxTeams = eventEntity.MaxTeams,
                CreatedTime = eventEntity.CreatedTime,
                LastUpdatedTime = eventEntity.LastUpdatedTime,
                Rounds = eventEntity.Rounds.Select(r => new EventRoundModel
                {
                    Id = r.Id,
                    RoundName = r.RoundName,
                    RoundNumber = r.RoundNumber,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    AdvancementRule = r.AdvancementRule,
                    ScoringStartDate = r.ScoringStartDate,
                    ScoringEndDate = r.ScoringEndDate,
                    Tracks = r.Tracks.Select(t => new EventTrackModel
                    {
                        Id = t.Id,
                        TrackName = t.TrackName,
                        Description = t.Description,
                        TemplateId = t.TemplateId,
                        SubmissionRuleDescription = t.SubmissionRuleDescription,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        ScoringStartDate = t.ScoringStartDate,
                        ScoringEndDate = t.ScoringEndDate
                    }).ToList()
                }).ToList(),
                Prizes = eventEntity.Prizes.Select(p => new SEAL_Application.Features.Prizes.Models.PrizeModel
                {
                    Id = p.Id,
                    EventId = p.EventId,
                    PrizeName = p.PrizeName,
                    Value = p.Value,
                    Quantity = p.Quantity
                }).ToList()
            };
        }
    }
}

