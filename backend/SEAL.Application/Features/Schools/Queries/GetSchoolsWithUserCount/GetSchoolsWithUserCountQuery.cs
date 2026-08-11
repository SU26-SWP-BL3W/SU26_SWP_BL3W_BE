using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Schools.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Schools.Queries.GetSchoolsWithUserCount
{
    public class GetSchoolsWithUserCountQuery : IRequest<Result<IEnumerable<SchoolWithUserCountModel>>>
    {
    }
}

