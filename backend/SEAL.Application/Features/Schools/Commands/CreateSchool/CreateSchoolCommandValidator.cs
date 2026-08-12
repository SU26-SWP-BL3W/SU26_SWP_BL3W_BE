using FluentValidation;

namespace SEAL_Application.Features.Schools.Commands.CreateSchool
{
    public class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
    {
        public CreateSchoolCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu tạo mới không được để trống");

            RuleFor(x => x.Model.SchoolName)
                .NotEmpty().WithMessage("Tên trường không được để trống")
                .MaximumLength(255).WithMessage("Tên trường không được vượt quá 255 ký tự")
                .When(x => x.Model != null);
        }
    }
}
