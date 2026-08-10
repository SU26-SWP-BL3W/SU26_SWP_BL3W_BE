using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.RespondTeamInvitation
{
    public class RespondTeamInvitationCommandValidator : AbstractValidator<RespondTeamInvitationCommand>
    {
        public RespondTeamInvitationCommandValidator()
        {
            RuleFor(x => x.InvitationId)
                .NotEmpty().WithMessage("ID lời mời không được để trống.");
        }
    }
}
