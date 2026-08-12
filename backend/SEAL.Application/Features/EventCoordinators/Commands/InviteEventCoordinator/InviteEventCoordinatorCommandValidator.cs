using FluentValidation;

namespace SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator
{
    public class InviteEventCoordinatorCommandValidator : AbstractValidator<InviteEventCoordinatorCommand>
    {
        public InviteEventCoordinatorCommandValidator()
        {
            RuleFor(x => x.Model.EventId)
                .NotEmpty().WithMessage("EventId là bắt buộc.");

            RuleFor(x => x.Model.CoordinatorEmail)
                .NotEmpty().WithMessage("Email của Event Coordinator là bắt buộc.")
                .EmailAddress().WithMessage("Email không hợp lệ.");

            RuleFor(x => x.Model.CoordinatorFullName)
                .NotEmpty().WithMessage("Họ tên của Event Coordinator là bắt buộc.")
                .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự.");
        }
    }
}
