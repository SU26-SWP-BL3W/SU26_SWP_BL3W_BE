using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.RejectTeamRegistration
{
    public class RejectTeamRegistrationCommandValidator : AbstractValidator<RejectTeamRegistrationCommand>
    {
        public RejectTeamRegistrationCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("Mã đội (TeamId) không được để trống.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Vui lòng nhập lý do từ chối để đội biết cần sửa gì.")
                .MaximumLength(1000).WithMessage("Lý do từ chối không vượt quá 1000 ký tự.");
        }
    }
}
