using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Teams.Commands.RemoveTeamMember
{
    public class RemoveTeamMemberCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

        public RemoveTeamMemberCommand(string teamId, string userId)
        {
            TeamId = teamId;
            UserId = userId;
        }
    }
}

