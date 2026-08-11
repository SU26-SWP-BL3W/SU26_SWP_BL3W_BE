using FluentValidation;

namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults
{
    public class CalculateRoundResultsCommandValidator : AbstractValidator<CalculateRoundResultsCommand>
    {
        public CalculateRoundResultsCommandValidator()
        {
            RuleFor(x => x.RoundId)
                .NotEmpty().WithMessage("ID vòng thi không được để trống.");

            RuleFor(x => x.TopN)
                .GreaterThanOrEqualTo(1).WithMessage("Số đội thăng vòng (TopN) phải lớn hơn hoặc bằng 1.")
                .When(x => x.TopN != 0); // Chỉ validate khi có truyền giá trị (khác 0)
        }
    }
}
