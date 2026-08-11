using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Domain.Entity
{
    public class Appeal : BaseEntity
    {
        public string TeamId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Response { get; set; }
        public AppealStatus Status { get; set; } = AppealStatus.Pending;
        public string? AssignedJudgeId { get; set; }

        // Navigation Properties
        public virtual Team Team { get; set; } = null!;
        public virtual SubmitResult SubmitResult { get; set; } = null!;
    }
}
