using FluentValidation;

namespace SEAL_Application.Features.Scores.Commands.SaveScore
{
    public class SaveScoreCommandValidator : AbstractValidator<SaveScoreCommand>
    {
        public SaveScoreCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu phiếu chấm không được để trống");

            RuleFor(x => x.Model.EventRoleId)
                .NotEmpty().WithMessage("ID vai trò chấm điểm (EventRoleId) không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.SubmitResultId)
                .NotEmpty().WithMessage("ID bài nộp (SubmitResultId) không được để trống")
                .When(x => x.Model != null);

            RuleForEach(x => x.Model.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.TemplateId)
                    .NotEmpty().WithMessage("TemplateId của điểm chi tiết không được để trống");
                detail.RuleFor(d => d.CriteriaId)
                    .NotEmpty().WithMessage("CriteriaId của điểm chi tiết không được để trống");
                detail.RuleFor(d => d.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("Điểm chi tiết phải lớn hơn hoặc bằng 0");
            }).When(x => x.Model != null);
        }
    }
}
