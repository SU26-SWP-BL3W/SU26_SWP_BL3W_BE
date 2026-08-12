using FluentValidation;

namespace SEAL_Application.Features.FinalResults.Commands.CreateFinalResult
{
    public class CreateFinalResultCommandValidator : AbstractValidator<CreateFinalResultCommand>
    {
        public CreateFinalResultCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu kết quả chung cuộc không được để trống");

            RuleFor(x => x.Model.TeamId)
                .NotEmpty().WithMessage("ID đội thi không được để trống")
                .When(x => x.Model != null);

            // RoundId/EventId/TrackId: phải có đúng 1 (kiểm tra chi tiết trong handler)
            RuleFor(x => x.Model)
                .Must(m => (!string.IsNullOrEmpty(m.RoundId) ? 1 : 0)
                         + (!string.IsNullOrEmpty(m.EventId) ? 1 : 0)
                         + (!string.IsNullOrEmpty(m.TrackId) ? 1 : 0) == 1)
                .WithMessage("Phải nhập đúng MỘT trong: RoundId, EventId hoặc TrackId.")
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
