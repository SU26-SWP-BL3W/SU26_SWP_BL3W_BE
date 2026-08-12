using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Criterias.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Criterias.Queries.GetAllCriteria
{
    public class GetAllCriteriaQuery : BasePaginationQuery, IRequest<Result<PagedResult<CriteriaModel>>>
    {
        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "CriteriaName", "CreatedTime" };
    }
}

