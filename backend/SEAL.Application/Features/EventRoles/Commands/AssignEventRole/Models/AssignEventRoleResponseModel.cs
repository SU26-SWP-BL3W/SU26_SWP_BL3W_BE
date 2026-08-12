using System;

namespace SEAL_Application.Features.EventRoles.Commands.AssignEventRole.Models
{
    public class AssignEventRoleResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? AssignedAt { get; set; }
    }
}
