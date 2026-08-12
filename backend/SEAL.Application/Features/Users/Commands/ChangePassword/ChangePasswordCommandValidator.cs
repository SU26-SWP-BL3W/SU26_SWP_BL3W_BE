using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.Model.OldPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu cũ.");

            RuleFor(x => x.Model.NewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.");

            RuleFor(x => x.Model.ConfirmNewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập xác nhận mật khẩu.")
                .Equal(x => x.Model.NewPassword).WithMessage("Mật khẩu xác nhận không khớp với mật khẩu mới.");
        }
    }
}
