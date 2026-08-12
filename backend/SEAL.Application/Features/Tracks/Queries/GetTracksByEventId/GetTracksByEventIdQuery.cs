using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Tracks.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Tracks.Queries.GetTracksByEventId
{
    public class GetTracksByEventIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<TrackModel>>>
    {
        public string EventId { get; set; } = string.Empty;

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "TrackName",
            "CreatedTime",
            "RoundId"
        };
    }
}

