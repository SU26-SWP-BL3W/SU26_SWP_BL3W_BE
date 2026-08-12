using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.CreateUser.Models;

namespace SEAL_Application.Features.Users.Commands.CreateUser
{
    public class CreateUserCommand : IRequest<Result<CreateUserResponseModel>>
    {
        public CreateUserRequestModel Model { get; set; } = new CreateUserRequestModel();
    }
}

