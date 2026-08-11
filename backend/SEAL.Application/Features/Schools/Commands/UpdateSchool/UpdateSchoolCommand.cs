using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Schools.Commands.UpdateSchool.Models;

namespace SEAL_Application.Features.Schools.Commands.UpdateSchool
{
    public class UpdateSchoolCommand : IRequest<Result<UpdateSchoolResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateSchoolRequestModel Model { get; set; } = new UpdateSchoolRequestModel();
    }
}

