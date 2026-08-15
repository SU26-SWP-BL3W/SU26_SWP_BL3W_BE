using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.CancelTeamInvitation
{
    public class CancelTeamInvitationCommandValidator : AbstractValidator<CancelTeamInvitationCommand>
    {
        public CancelTeamInvitationCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("TeamId không được để trống.");

            RuleFor(x => x.InvitationId)
                .NotEmpty().WithMessage("InvitationId không được để trống.");
        }
    }
}
