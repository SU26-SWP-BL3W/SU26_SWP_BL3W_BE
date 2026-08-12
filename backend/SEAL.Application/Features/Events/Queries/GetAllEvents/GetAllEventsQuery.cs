using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Events.Models;

namespace SEAL_Application.Features.Events.Queries.GetAllEvents
{
    public class GetAllEventsQuery : BasePaginationQuery, IRequest<Result<PagedResult<EventModel>>>
    {
        public string? SearchName { get; set; }
        public bool? Status { get; set; }

        public override System.Collections.Generic.HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "EventName",
            "StartDate",
            "EndDate",
            "Year"
        };
    }
}

