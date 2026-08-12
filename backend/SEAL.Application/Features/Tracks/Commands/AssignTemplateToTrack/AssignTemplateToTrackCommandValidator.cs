using FluentValidation;

namespace SEAL_Application.Features.Tracks.Commands.AssignTemplateToTrack
{
    public class AssignTemplateToTrackCommandValidator : AbstractValidator<AssignTemplateToTrackCommand>
    {
        public AssignTemplateToTrackCommandValidator()
        {
            RuleFor(x => x.TrackId)
                .NotEmpty().WithMessage("ID hạng mục không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu không được để trống");

            RuleFor(x => x.Model.TemplateId)
                .NotEmpty().WithMessage("ID mẫu tiêu chí không được để trống")
                .When(x => x.Model != null);
        }
    }
}
