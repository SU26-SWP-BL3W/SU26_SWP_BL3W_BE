using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration
{
    public class ConfirmTeamRegistrationCommandValidator : AbstractValidator<ConfirmTeamRegistrationCommand>
    {
        public ConfirmTeamRegistrationCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("ID nhóm không được để trống.");
        }
    }
}
