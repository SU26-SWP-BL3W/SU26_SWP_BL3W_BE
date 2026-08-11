using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.ApproveTeamRegistration
{
    public class ApproveTeamRegistrationCommandValidator : AbstractValidator<ApproveTeamRegistrationCommand>
    {
        public ApproveTeamRegistrationCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("Mã đội (TeamId) không được để trống.");
        }
    }
}
