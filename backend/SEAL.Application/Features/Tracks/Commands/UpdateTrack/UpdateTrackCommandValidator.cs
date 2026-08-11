using FluentValidation;

namespace SEAL_Application.Features.Tracks.Commands.UpdateTrack
{
    public class UpdateTrackCommandValidator : AbstractValidator<UpdateTrackCommand>
    {
        public UpdateTrackCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID hạng mục không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu cập nhật không được để trống");

            RuleFor(x => x.Model.RoundId)
                .NotEmpty().WithMessage("ID vòng thi không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.TrackName)
                .NotEmpty().WithMessage("Tên hạng mục không được để trống")
                .MaximumLength(255).WithMessage("Tên hạng mục không được vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Description));

            RuleFor(x => x.Model)
                .Must(r => !r.StartDate.HasValue || !r.EndDate.HasValue || r.StartDate.Value < r.EndDate.Value)
                .WithMessage("Thời gian bắt đầu nộp bài phải nhỏ hơn hạn nộp bài.")
                .Must(r => !r.ScoringStartDate.HasValue || !r.EndDate.HasValue || r.ScoringStartDate.Value >= r.EndDate.Value)
                .WithMessage("Thời gian bắt đầu chấm phải sau hoặc cùng lúc với hạn nộp bài.")
                .Must(r => !r.ScoringEndDate.HasValue || r.ScoringEndDate.Value > (r.ScoringStartDate ?? r.EndDate ?? System.DateTime.MinValue))
                .WithMessage("Hạn chót chấm phải sau thời điểm bắt đầu chấm.")
                .When(x => x.Model != null);
        }
    }
}
