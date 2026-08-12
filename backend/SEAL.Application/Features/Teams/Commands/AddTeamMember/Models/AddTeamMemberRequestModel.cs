namespace SEAL_Application.Features.Teams.Commands.AddTeamMember.Models
{
    public class AddTeamMemberRequestModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
