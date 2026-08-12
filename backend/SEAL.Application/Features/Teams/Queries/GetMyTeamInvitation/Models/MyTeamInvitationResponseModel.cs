using System;

namespace SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation.Models
{
    public class MyTeamInvitationResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string InvitedByUserId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? Notes { get; set; }
    }
}
