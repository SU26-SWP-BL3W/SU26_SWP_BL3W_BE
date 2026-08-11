using SEAL_Domain.Base;
using System.Collections.Generic;
namespace SEAL_Domain.Entity
{
    public class Track : BaseEntity
    {
        public string RoundId { get; set; } = string.Empty;
        public string? TemplateId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SubmissionRuleDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ScoringStartDate { get; set; }
        public DateTime? ScoringEndDate { get; set; }

        // Navigation Properties
        public virtual Round Round { get; set; } = null!;
        public virtual Template? Template { get; set; }
        public virtual ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();
    }
}
