using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Schools.Models;

namespace SEAL_Application.Features.Schools.Queries.GetSchoolById
{
    public class GetSchoolByIdQuery : IRequest<Result<SchoolModel?>>
    {
        public string Id { get; set; } = string.Empty;
    }
}

