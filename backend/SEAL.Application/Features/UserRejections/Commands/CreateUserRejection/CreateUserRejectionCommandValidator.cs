using FluentValidation;

namespace SEAL_Application.Features.UserRejections.Commands.CreateUserRejection
{
    public class CreateUserRejectionCommandValidator : AbstractValidator<CreateUserRejectionCommand>
    {
        public CreateUserRejectionCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu từ chối không được để trống");

            RuleFor(x => x.Model.UserId)
                .NotEmpty().WithMessage("ID người dùng bị từ chối không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.RejectedBy)
                .NotEmpty().WithMessage("ID người thực hiện từ chối không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Reason)
                .MaximumLength(500).WithMessage("Lý do từ chối không được vượt quá 500 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Reason));
        }
    }
}
