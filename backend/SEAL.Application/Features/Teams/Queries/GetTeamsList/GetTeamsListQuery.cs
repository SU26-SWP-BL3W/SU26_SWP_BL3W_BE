using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Queries.GetTeamsList.Models;
using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Teams.Queries.GetTeamsList
{
    public class GetTeamsListQuery : BasePaginationQuery, IRequest<Result<PagedResult<TeamListItemModel>>>
    {
        public string? SearchName { get; set; }
        public bool? MyTeamsOnly { get; set; }
        public string? EventId { get; set; }

        public override HashSet<string> GetAllowedSortFields() => new(StringComparer.OrdinalIgnoreCase)
        {
            "Name",
            "CreatedTime"
        };
    }
}

