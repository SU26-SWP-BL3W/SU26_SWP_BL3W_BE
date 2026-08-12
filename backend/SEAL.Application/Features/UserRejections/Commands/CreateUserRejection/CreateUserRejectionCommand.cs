using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.UserRejections.Commands.CreateUserRejection.Models;

namespace SEAL_Application.Features.UserRejections.Commands.CreateUserRejection
{
    public class CreateUserRejectionCommand : IRequest<Result<CreateUserRejectionResponseModel>>
    {
        public CreateUserRejectionRequestModel Model { get; set; } = new CreateUserRejectionRequestModel();
    }
}
