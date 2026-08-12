using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.ScoreDetails.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.ScoreDetails.Queries.GetScoreDetailsByScoreId
{
    public class GetScoreDetailsByScoreIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<ScoreDetailModel>>>
    {
        public string ScoreId { get; set; } = string.Empty;

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "Value",
            "CreatedTime",
            "LastUpdatedTime"
        };
    }
}

