using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Rounds.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Rounds.Queries.GetRoundsByEventId
{
    public class GetRoundsByEventIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<RoundModel>>>
    {
        public string EventId { get; set; } = string.Empty;

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "RoundName",
            "RoundNumber",
            "StartDate",
            "CreatedTime"
        };
    }
}

