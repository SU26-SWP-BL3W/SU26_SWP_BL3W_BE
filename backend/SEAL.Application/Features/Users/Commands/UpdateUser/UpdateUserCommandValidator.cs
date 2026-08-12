using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.UpdateUser
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID người dùng không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu cập nhật không được để trống");

            RuleFor(x => x.Model.SchoolId)
                .NotEmpty().WithMessage("School ID không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(255).WithMessage("Họ và tên không vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.StudentCode)
                .NotEmpty().WithMessage("Mã sinh viên là bắt buộc đối với sinh viên")
                .When(x => x.Model != null && x.Model.IsStudent);
        }
    }
}
