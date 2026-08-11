using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Domain.Entity
{
    public class EventRole : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TeamId { get; set; }
        public string? TrackId { get; set; }
        
        public EventRoleType RoleName { get; set; }
        
        public DateTime? AssignedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Event Event { get; set; } = null!;
        public virtual Track? Track { get; set; }
        public virtual Team? Team { get; set; }
    }
}
