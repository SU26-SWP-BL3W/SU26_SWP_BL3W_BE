using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Users.Commands.ResendEmailVerification
{
    public class ResendEmailVerificationCommand : IRequest<Result<bool>>
    {
        public string Email { get; set; } = string.Empty;
    }
}
