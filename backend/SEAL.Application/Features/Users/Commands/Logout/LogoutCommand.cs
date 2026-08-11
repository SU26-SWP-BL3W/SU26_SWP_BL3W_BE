using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Users.Commands.Logout
{
    public class LogoutCommand : IRequest<Result<bool>>
    {
        public string UserId { get; }

        public LogoutCommand(string userId)
        {
            UserId = userId;
        }
    }
}

