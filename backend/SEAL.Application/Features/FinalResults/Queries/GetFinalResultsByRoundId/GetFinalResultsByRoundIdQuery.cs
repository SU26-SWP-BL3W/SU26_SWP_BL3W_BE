using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.FinalResults.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.FinalResults.Queries.GetFinalResultsByRoundId
{
    public class GetFinalResultsByRoundIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<FinalResultModel>>>
    {
        public string RoundId { get; set; } = string.Empty;

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "Rank",
            "FinalScore",
            "CreatedTime",
            "LastUpdatedTime"
        };
    }
}

