using FluentValidation;

namespace SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate
{
    public class AddCriteriaToTemplateCommandValidator : AbstractValidator<AddCriteriaToTemplateCommand>
    {
        public AddCriteriaToTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("ID mẫu tiêu chí không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu không được để trống");

            RuleFor(x => x.Model.CriteriaId)
                .NotEmpty().WithMessage("ID tiêu chí không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Weight)
                .GreaterThan(0).WithMessage("Trọng số phải lớn hơn 0")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.MaxScore)
                .GreaterThan(0).WithMessage("Điểm số tối đa phải lớn hơn 0")
                .When(x => x.Model != null);
        }
    }
}
