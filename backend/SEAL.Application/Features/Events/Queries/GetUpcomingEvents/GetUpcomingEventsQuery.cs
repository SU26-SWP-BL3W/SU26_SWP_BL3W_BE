using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Events.Models;

namespace SEAL_Application.Features.Events.Queries.GetUpcomingEvents
{
    public class GetUpcomingEventsQuery : BasePaginationQuery, IRequest<Result<PagedResult<EventModel>>>
    {
        public override System.Collections.Generic.HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "EventName",
            "StartDate",
            "EndDate"
        };
    }
}

