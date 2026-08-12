using FluentValidation;

namespace SEAL_Application.Features.Criterias.Commands.CreateCriteria
{
    public class CreateCriteriaCommandValidator : AbstractValidator<CreateCriteriaCommand>
    {
        public CreateCriteriaCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu tiêu chí không được để trống");

            RuleFor(x => x.Model.CriteriaName)
                .NotEmpty().WithMessage("Tên tiêu chí không được để trống")
                .MaximumLength(255).WithMessage("Tên tiêu chí không được vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Description));
        }
    }
}
