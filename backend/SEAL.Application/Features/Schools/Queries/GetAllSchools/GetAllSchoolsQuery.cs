using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Schools.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Schools.Queries.GetAllSchools
{
    public class GetAllSchoolsQuery : BasePaginationQuery, IRequest<Result<PagedResult<SchoolModel>>>
    {
        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "SchoolName", "Address" };
    }
}

