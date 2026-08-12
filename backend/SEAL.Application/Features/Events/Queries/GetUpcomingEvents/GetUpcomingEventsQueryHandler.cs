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

namespace SEAL_Application.Features.Events.Queries.GetUpcomingEvents
{
    public class GetUpcomingEventsQueryHandler : IRequestHandler<GetUpcomingEventsQuery, Result<PagedResult<EventModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUpcomingEventsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<EventModel>>> Handle(GetUpcomingEventsQuery request, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var query = _unitOfWork.GetRepository<Event>().Entities
                .Where(e => e.StartDate > utcNow && e.Status == true);

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
                    CreatedTime = e.CreatedTime,
                    LastUpdatedTime = e.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedEvents;
        }
    }
}



