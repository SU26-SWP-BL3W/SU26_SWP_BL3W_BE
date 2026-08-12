using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(v => v.Model.Email)
                .NotEmpty().WithMessage("Email là bắt buộc.")
                .EmailAddress().WithMessage("Email không hợp lệ.");

            RuleFor(v => v.Model.Password)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc.");
        }
    }
}
