using FluentValidation;

namespace SEAL_Application.Features.Mentors.Commands.InviteMentorToTrack
{
    public class InviteMentorToTrackCommandValidator : AbstractValidator<InviteMentorToTrackCommand>
    {
        public InviteMentorToTrackCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu mời mentor không được để trống");

            RuleFor(x => x.Model.EventId)
                .NotEmpty().WithMessage("EventId không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.TrackId)
                .NotEmpty().WithMessage("TrackId (hạng mục cần hướng dẫn) không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.MentorEmail)
                .NotEmpty().WithMessage("Email mentor không được để trống")
                .EmailAddress().WithMessage("Email mentor không hợp lệ")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.MentorFullName)
                .NotEmpty().WithMessage("Họ tên mentor không được để trống")
                .When(x => x.Model != null);
        }
    }
}
