using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.LoginUser.Models;

namespace SEAL_Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommand : IRequest<Result<LoginUserResponseModel>>
    {
        public LoginUserRequestModel Model { get; set; } = new LoginUserRequestModel();
    }
}

