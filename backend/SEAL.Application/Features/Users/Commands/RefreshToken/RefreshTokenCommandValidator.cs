using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.Model.RefreshToken)
                .NotEmpty().WithMessage("RefreshToken is required.");
        }
    }
}
