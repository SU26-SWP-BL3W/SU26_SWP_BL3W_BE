namespace SEAL_Application.Features.Teams.Commands.RespondTeamInvitation.Models
{
    public class RespondTeamInvitationResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>ID của EventRole vừa tạo nếu member chấp nhận (role = TeamMember).</summary>
        public string? EventRoleId { get; set; }
    }
}
