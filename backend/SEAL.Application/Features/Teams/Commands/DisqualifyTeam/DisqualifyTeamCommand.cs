using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Teams.Commands.DisqualifyTeam
{
    public class DisqualifyTeamCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        public DisqualifyTeamCommand(string teamId, string reason)
        {
            TeamId = teamId;
            Reason = reason;
        }
    }
}
