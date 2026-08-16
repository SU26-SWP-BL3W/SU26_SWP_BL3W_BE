using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class MentorFeedback : BaseEntity
    {
        public string SubmitResultId { get; set; } = string.Empty;
        public string EventRoleId { get; set; } = string.Empty;
        public string FeedbackText { get; set; } = string.Empty;

        // Navigation Properties
        public virtual SubmitResult SubmitResult { get; set; } = null!;
        public virtual EventRole EventRole { get; set; } = null!;
    }
}
