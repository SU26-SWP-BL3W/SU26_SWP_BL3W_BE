using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.RegisterUser.Models;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<Result<UserModel>>
    {
        public RegisterUserRequestModel Model { get; set; } = new RegisterUserRequestModel();
    }
}

