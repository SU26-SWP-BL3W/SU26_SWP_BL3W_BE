using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.DisqualifyTeam
{
    public class DisqualifyTeamCommandValidator : AbstractValidator<DisqualifyTeamCommand>
    {
        public DisqualifyTeamCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("Mã đội (TeamId) không được để trống.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Vui lòng nhập lý do loại đội.")
                .MaximumLength(1000).WithMessage("Lý do loại đội không vượt quá 1000 ký tự.");
        }
    }
}
