using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Teams.Commands.CancelTeamInvitation
{
    public class CancelTeamInvitationCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;
        public string InvitationId { get; set; } = string.Empty;
    }
}
