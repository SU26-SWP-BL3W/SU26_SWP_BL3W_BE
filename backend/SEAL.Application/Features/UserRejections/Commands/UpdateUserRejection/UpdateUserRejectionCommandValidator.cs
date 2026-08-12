using FluentValidation;

namespace SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection
{
    public class UpdateUserRejectionCommandValidator : AbstractValidator<UpdateUserRejectionCommand>
    {
        public UpdateUserRejectionCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID bản ghi từ chối không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu cập nhật không được để trống");

            RuleFor(x => x.Model.Reason)
                .NotEmpty().WithMessage("Lý do từ chối không được để trống")
                .MaximumLength(500).WithMessage("Lý do từ chối không được vượt quá 500 ký tự")
                .When(x => x.Model != null);
        }
    }
}
