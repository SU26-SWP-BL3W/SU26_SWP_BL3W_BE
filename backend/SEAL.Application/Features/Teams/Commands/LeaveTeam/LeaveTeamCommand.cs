using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Teams.Commands.LeaveTeam
{
    public class LeaveTeamCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;

        public LeaveTeamCommand(string teamId)
        {
            TeamId = teamId;
        }
    }
}

