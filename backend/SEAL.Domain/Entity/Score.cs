using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Domain.Entity
{
    public class Score : BaseEntity
    {
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? Comment { get; set; }
        public bool IsSubmitted { get; set; } = false;

        // Navigation Properties
        public virtual EventRole EventRole { get; set; } = null!;
        public virtual SubmitResult SubmitResult { get; set; } = null!;
        public virtual ICollection<ScoreDetail> ScoreDetails { get; set; } = new List<ScoreDetail>();
    }
}
