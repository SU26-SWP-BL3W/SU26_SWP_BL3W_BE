using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommand : IRequest<Result<bool>>
    {
        public string Token { get; set; } = string.Empty;
    }
}

