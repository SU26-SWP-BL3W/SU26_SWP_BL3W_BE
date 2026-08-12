namespace SEAL_Application.Features.Teams.Commands.InviteTeamMember.Models
{
    public class InviteTeamMemberRequestModel
    {
        /// <summary>Email người dùng được mời vào đội.</summary>
        public string Email { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
