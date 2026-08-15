using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.ResendEmailVerification
{
    public class ResendEmailVerificationCommandValidator : AbstractValidator<ResendEmailVerificationCommand>
    {
        public ResendEmailVerificationCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}
