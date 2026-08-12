using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models;

namespace SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation
{
    public class RespondEventRoleInvitationCommand : IRequest<Result<RespondEventRoleInvitationResponseModel>>
    {
        public string InvitationId { get; set; } = string.Empty;

        /// <summary>true = chấp nhận (Accept), false = từ chối (Reject).</summary>
        public bool IsAccepted { get; set; }

        public RespondEventRoleInvitationCommand(string invitationId, bool isAccepted)
        {
            InvitationId = invitationId;
            IsAccepted = isAccepted;
        }
    }
}

