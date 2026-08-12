using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.RefreshToken.Models;

namespace SEAL_Application.Features.Users.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<Result<RefreshTokenResponseModel>>
    {
        public RefreshTokenRequestModel Model { get; }

        public RefreshTokenCommand(RefreshTokenRequestModel model)
        {
            Model = model;
        }
    }
}

