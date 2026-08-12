using System;

namespace SEAL_Application.Features.EventRoles.Commands.InviteEventRole.Models
{
    public class InviteEventRoleResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string InvitedUserId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
