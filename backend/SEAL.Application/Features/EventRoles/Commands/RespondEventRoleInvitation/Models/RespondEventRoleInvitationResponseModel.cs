namespace SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models
{
    public class RespondEventRoleInvitationResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>ID của EventRole vừa tạo nếu người được mời chấp nhận.</summary>
        public string? EventRoleId { get; set; }
    }
}
