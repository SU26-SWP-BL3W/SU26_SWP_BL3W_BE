using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Schools.Commands.DeleteSchool
{
    public class DeleteSchoolCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; } = string.Empty;
    }
}

