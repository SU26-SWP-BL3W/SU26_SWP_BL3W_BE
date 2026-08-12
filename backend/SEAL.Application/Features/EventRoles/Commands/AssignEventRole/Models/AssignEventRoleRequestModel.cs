using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Application.Features.EventRoles.Commands.AssignEventRole.Models
{
    public class AssignEventRoleRequestModel
    {
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string? TeamId { get; set; }
        public EventRoleType RoleName { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public string? Notes { get; set; }
    }
}
