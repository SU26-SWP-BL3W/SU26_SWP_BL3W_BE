using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Schools.Commands.CreateSchool.Models;

namespace SEAL_Application.Features.Schools.Commands.CreateSchool
{
    public class CreateSchoolCommand : IRequest<Result<CreateSchoolResponseModel>>
    {
        public CreateSchoolRequestModel Model { get; set; } = new CreateSchoolRequestModel();
    }
}

