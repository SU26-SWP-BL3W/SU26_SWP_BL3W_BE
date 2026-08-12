using System;

namespace SEAL_Application.Features.EventRoles.Commands.UpdateEventRole.Models
{
    public class UpdateEventRoleResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? ExpiredAt { get; set; }
    }
}
