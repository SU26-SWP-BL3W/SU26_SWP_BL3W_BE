using SEAL_Domain.Base;
using System;
using System.Collections.Generic;

namespace SEAL_Domain.Entity
{
    public class Event : BaseEntity
    {
        public string EventName { get; set; } = string.Empty;
        public string? Season { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
        public string? PhotoEventUrl { get; set; }
        public int MaxTeams { get; set; }

        // Navigation Properties
        public virtual ICollection<Round> Rounds { get; set; } = new List<Round>();
        public virtual ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();
        public virtual ICollection<Prize> Prizes { get; set; } = new List<Prize>();
    }
}
