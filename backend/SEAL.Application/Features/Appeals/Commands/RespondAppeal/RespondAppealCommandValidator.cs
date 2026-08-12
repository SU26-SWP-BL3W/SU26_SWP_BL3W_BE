using FluentValidation;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Features.Appeals.Commands.RespondAppeal
{
    public class RespondAppealCommandValidator : AbstractValidator<RespondAppealCommand>
    {
        public RespondAppealCommandValidator()
        {
            RuleFor(x => x.AppealId)
                .NotEmpty().WithMessage("ID đơn phúc khảo không được để trống.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Trạng thái không hợp lệ.")
                .Must(s => s == AppealStatus.Approved || s == AppealStatus.Rejected)
                .WithMessage("Trạng thái phản hồi chỉ có thể là Approved hoặc Rejected.");

            RuleFor(x => x.Response)
                .NotEmpty().WithMessage("Nội dung phản hồi không được để trống.")
                .MaximumLength(1000).WithMessage("Nội dung phản hồi không được vượt quá 1000 ký tự.");
        }
    }
}
