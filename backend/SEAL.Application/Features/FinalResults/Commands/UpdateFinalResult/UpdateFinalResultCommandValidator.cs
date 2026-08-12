using FluentValidation;

namespace SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult
{
    public class UpdateFinalResultCommandValidator : AbstractValidator<UpdateFinalResultCommand>
    {
        public UpdateFinalResultCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID kết quả chung cuộc không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu kết quả chung cuộc không được để trống");

            RuleFor(x => x.Model.TeamId)
                .NotEmpty().WithMessage("ID đội thi không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.RoundId)
                .NotEmpty().WithMessage("ID vòng thi không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.FinalScore)
                .GreaterThanOrEqualTo(0).WithMessage("Tổng điểm phải lớn hơn hoặc bằng 0")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Rank)
                .GreaterThanOrEqualTo(1).WithMessage("Thứ hạng phải lớn hơn hoặc bằng 1")
                .When(x => x.Model != null);
        }
    }
}
