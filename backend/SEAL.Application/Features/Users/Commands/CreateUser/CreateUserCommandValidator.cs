using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.CreateUser
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu tạo mới không được để trống");

            RuleFor(x => x.Model.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(255).WithMessage("Họ và tên không vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(6).WithMessage("Mật khẩu phải có tối thiểu 6 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.SchoolId)
                .NotEmpty().WithMessage("School ID không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.StudentCode)
                .NotEmpty().WithMessage("Mã sinh viên là bắt buộc đối với sinh viên")
                .When(x => x.Model != null && x.Model.IsStudent);
        }
    }
}
