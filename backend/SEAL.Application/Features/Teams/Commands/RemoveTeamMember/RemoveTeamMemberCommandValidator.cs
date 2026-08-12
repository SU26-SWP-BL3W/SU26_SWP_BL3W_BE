using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.RemoveTeamMember
{
    public class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
    {
        public RemoveTeamMemberCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("ID nhóm không được để trống.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("ID thành viên cần xóa không được để trống.");
        }
    }
}
