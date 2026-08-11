using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Application.Features.EventRoles.Commands.UpdateEventRole.Models
{
    public class UpdateEventRoleRequestModel
    {
        public string? TrackId { get; set; }
        public string? TeamId { get; set; }
        public EventRoleType RoleName { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public string? Notes { get; set; }
    }
}
