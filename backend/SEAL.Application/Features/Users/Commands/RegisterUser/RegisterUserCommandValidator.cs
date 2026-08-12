using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(v => v.Model.Email)
                .NotEmpty().WithMessage("Email là bắt buộc.")
                .EmailAddress().WithMessage("Email không hợp lệ.");

            RuleFor(v => v.Model.Password)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

            RuleFor(v => v.Model.FullName)
                .NotEmpty().WithMessage("Họ và tên là bắt buộc.")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");
        }
    }
}
