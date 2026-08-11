using FluentValidation;

namespace SEAL_Application.Features.Scores.Commands.CreateScore
{
    public class CreateScoreCommandValidator : AbstractValidator<CreateScoreCommand>
    {
        public CreateScoreCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu phiếu chấm không được để trống");

            RuleFor(x => x.Model.EventRoleId)
                .NotEmpty().WithMessage("ID vai trò chấm điểm không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.SubmitResultId)
                .NotEmpty().WithMessage("ID bài nộp không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Comment)
                .MaximumLength(1000).WithMessage("Nhận xét không được vượt quá 1000 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Comment));
        }
    }
}
