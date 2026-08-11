using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Scores.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Scores.Queries.GetScoresByEventRoleId
{
    public class GetScoresByEventRoleIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<ScoreModel>>>
    {
        public string EventRoleId { get; set; } = string.Empty;

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "TotalScore",
            "CreatedTime",
            "LastUpdatedTime"
        };
    }
}

