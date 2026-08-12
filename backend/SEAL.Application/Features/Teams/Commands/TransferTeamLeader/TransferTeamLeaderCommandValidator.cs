using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.TransferTeamLeader
{
    public class TransferTeamLeaderCommandValidator : AbstractValidator<TransferTeamLeaderCommand>
    {
        public TransferTeamLeaderCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("ID nhóm không được để trống.");

            RuleFor(x => x.NewLeaderUserId)
                .NotEmpty().WithMessage("Phải chọn thành viên nhận quyền Trưởng nhóm.");
        }
    }
}
