using FluentValidation;

namespace SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack
{
    public class InviteJudgeToTrackCommandValidator : AbstractValidator<InviteJudgeToTrackCommand>
    {
        public InviteJudgeToTrackCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu mời giám khảo không được để trống");

            RuleFor(x => x.Model.EventId)
                .NotEmpty().WithMessage("EventId không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.TrackId)
                .NotEmpty().WithMessage("TrackId (hạng mục cần chấm) không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.JudgeEmail)
                .NotEmpty().WithMessage("Email giám khảo không được để trống")
                .EmailAddress().WithMessage("Email giám khảo không hợp lệ")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.JudgeFullName)
                .NotEmpty().WithMessage("Họ tên giám khảo không được để trống")
                .When(x => x.Model != null);
        }
    }
}
