using FluentValidation;

namespace SEAL_Application.Features.Appeals.Commands.CreateAppeal
{
    public class CreateAppealCommandValidator : AbstractValidator<CreateAppealCommand>
    {
        public CreateAppealCommandValidator()
        {
            RuleFor(x => x.SubmitResultId)
                .NotEmpty().WithMessage("ID bài nộp không được để trống.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Lý do phúc khảo không được để trống.")
                .MaximumLength(1000).WithMessage("Lý do phúc khảo không được vượt quá 1000 ký tự.");
        }
    }
}
