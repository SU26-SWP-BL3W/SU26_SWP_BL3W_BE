using FluentValidation;

namespace SEAL_Application.Features.Rounds.Commands.UpdateRound
{
    public class UpdateRoundCommandValidator : AbstractValidator<UpdateRoundCommand>
    {
        public UpdateRoundCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID vòng thi không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu cập nhật không được để trống");

            RuleFor(x => x.Model.EventId)
                .NotEmpty().WithMessage("ID sự kiện không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.RoundName)
                .NotEmpty().WithMessage("Tên vòng thi không được để trống")
                .MaximumLength(255).WithMessage("Tên vòng thi không được vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.RoundNumber)
                .GreaterThan(0).WithMessage("Số thứ tự vòng thi phải lớn hơn 0")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.AdvancementRule)
                .MaximumLength(1000).WithMessage("Quy tắc đi tiếp không được vượt quá 1000 ký tự")
                .Matches(@"^(top|percent|minScore)\s*:\s*\d+(\.\d+)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .WithMessage("Quy tắc đi tiếp phải có dạng 'top:N', 'percent:P' hoặc 'minScore:X' (ví dụ: top:3).")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.AdvancementRule));

            RuleFor(x => x.Model)
                .Must(m => m.StartDate < m.EndDate).WithMessage("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")
                .Must(m => !m.ScoringStartDate.HasValue || m.ScoringStartDate.Value >= m.EndDate)
                .WithMessage("Thời gian bắt đầu chấm phải sau khi kết thúc nộp bài.")
                .Must(m => !m.ScoringEndDate.HasValue || m.ScoringEndDate.Value > (m.ScoringStartDate ?? m.EndDate))
                .WithMessage("Hạn chót chấm phải sau thời điểm bắt đầu chấm.")
                .When(x => x.Model != null);
        }
    }
}
