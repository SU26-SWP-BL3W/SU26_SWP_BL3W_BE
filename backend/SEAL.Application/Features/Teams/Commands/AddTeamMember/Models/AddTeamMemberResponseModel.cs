using System;

namespace SEAL_Application.Features.Teams.Commands.AddTeamMember.Models
{
    public class AddTeamMemberResponseModel
    {
        public string EventRoleId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime? AssignedAt { get; set; }
    }
}
