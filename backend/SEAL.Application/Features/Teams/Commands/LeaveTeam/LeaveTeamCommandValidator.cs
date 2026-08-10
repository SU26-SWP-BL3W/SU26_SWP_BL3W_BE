using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.LeaveTeam
{
    public class LeaveTeamCommandValidator : AbstractValidator<LeaveTeamCommand>
    {
        public LeaveTeamCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("ID nhóm không được để trống.");
        }
    }
}
