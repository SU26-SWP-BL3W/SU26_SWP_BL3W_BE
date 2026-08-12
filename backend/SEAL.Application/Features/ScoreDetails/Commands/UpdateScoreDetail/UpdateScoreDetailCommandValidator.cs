using FluentValidation;

namespace SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail
{
    public class UpdateScoreDetailCommandValidator : AbstractValidator<UpdateScoreDetailCommand>
    {
        public UpdateScoreDetailCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID điểm chi tiết không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu điểm chi tiết không được để trống");

            RuleFor(x => x.Model.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Điểm phải lớn hơn hoặc bằng 0")
                .When(x => x.Model != null);
        }
    }
}
