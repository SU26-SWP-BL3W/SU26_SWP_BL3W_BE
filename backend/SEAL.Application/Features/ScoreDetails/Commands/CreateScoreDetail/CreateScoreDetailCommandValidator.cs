using FluentValidation;

namespace SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail
{
    public class CreateScoreDetailCommandValidator : AbstractValidator<CreateScoreDetailCommand>
    {
        public CreateScoreDetailCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu điểm chi tiết không được để trống");

            RuleFor(x => x.Model.ScoreId)
                .NotEmpty().WithMessage("ID phiếu chấm không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.TemplateId)
                .NotEmpty().WithMessage("ID mẫu tiêu chí không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.CriteriaId)
                .NotEmpty().WithMessage("ID tiêu chí không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Điểm phải lớn hơn hoặc bằng 0")
                .When(x => x.Model != null);
        }
    }
}
