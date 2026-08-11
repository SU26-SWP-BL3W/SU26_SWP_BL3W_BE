using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models;
using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Teams.Queries.GetMySubmissions
{
    public class GetMySubmissionsQuery : BasePaginationQuery, IRequest<Result<PagedResult<SubmitResultListItemModel>>>
    {
        public override HashSet<string> GetAllowedSortFields() => new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedTime"
        };
    }
}

