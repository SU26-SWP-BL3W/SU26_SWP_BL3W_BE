using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Commands.RespondTeamInvitation.Models;

namespace SEAL_Application.Features.Teams.Commands.RespondTeamInvitation
{
    public class RespondTeamInvitationCommand : IRequest<Result<RespondTeamInvitationResponseModel>>
    {
        public string InvitationId { get; set; } = string.Empty;

        /// <summary>true = chấp nhận (Accept), false = từ chối (Decline).</summary>
        public bool IsAccepted { get; set; }

        public RespondTeamInvitationCommand(string invitationId, bool isAccepted)
        {
            InvitationId = invitationId;
            IsAccepted = isAccepted;
        }
    }
}

