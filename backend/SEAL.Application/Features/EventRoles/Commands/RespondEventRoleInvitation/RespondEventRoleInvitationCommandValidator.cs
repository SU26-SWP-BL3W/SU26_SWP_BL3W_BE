using FluentValidation;

namespace SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation
{
    public class RespondEventRoleInvitationCommandValidator : AbstractValidator<RespondEventRoleInvitationCommand>
    {
        public RespondEventRoleInvitationCommandValidator()
        {
            RuleFor(x => x.InvitationId)
                .NotEmpty().WithMessage("ID lời mời không được để trống.");
        }
    }
}
