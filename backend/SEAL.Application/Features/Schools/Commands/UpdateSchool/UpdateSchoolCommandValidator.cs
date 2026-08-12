using FluentValidation;

namespace SEAL_Application.Features.Schools.Commands.UpdateSchool
{
    public class UpdateSchoolCommandValidator : AbstractValidator<UpdateSchoolCommand>
    {
        public UpdateSchoolCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu cập nhật không được để trống");

            RuleFor(x => x.Model.SchoolName)
                .NotEmpty().WithMessage("Tên trường không được để trống")
                .MaximumLength(255).WithMessage("Tên trường không được vượt quá 255 ký tự")
                .When(x => x.Model != null);
        }
    }
}
