using FluentValidation;

namespace SEAL_Application.Features.Templates.Commands.CreateTemplate
{
    public class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
    {
        public CreateTemplateCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu mẫu tiêu chí không được để trống");

            RuleFor(x => x.Model.TemplateName)
                .NotEmpty().WithMessage("Tên mẫu tiêu chí không được để trống")
                .MaximumLength(255).WithMessage("Tên mẫu tiêu chí không được vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Description));
        }
    }
}
