using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Events.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Events.Queries.GetAllEvents
{
    public class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, Result<PagedResult<EventModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllEventsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<EventModel>>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Event>().Entities;
            var teams = _unitOfWork.GetRepository<Team>().Entities;

            if (!string.IsNullOrWhiteSpace(request.SearchName))
            {
                query = query.Where(e => e.EventName.ToLower().Contains(request.SearchName.ToLower()));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(e => e.Status == request.Status.Value);
            }

            var pagedEvents = await query.ToPagedResultAsync(
                request,
                e => new EventModel
                {
                    Id = e.Id,
                    EventName = e.EventName,
                    Season = e.Season,
                    Year = e.Year,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    RegistrationStartDate = e.RegistrationStartDate,
                    RegistrationEndDate = e.RegistrationEndDate,
                    Description = e.Description,
                    Status = e.Status,
                    PhotoEventUrl = e.PhotoEventUrl,
                    MaxTeams = e.MaxTeams,
                    TeamCount = teams.Count(t => t.EventId == e.Id),
                    CreatedTime = e.CreatedTime,
                    LastUpdatedTime = e.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedEvents;
        }
    }
}



