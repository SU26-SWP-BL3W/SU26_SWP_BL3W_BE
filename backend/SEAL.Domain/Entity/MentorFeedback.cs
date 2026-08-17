using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class MentorFeedback : BaseEntity
    {
        public string SubmitResultId { get; set; } = string.Empty;
        public string MentorId { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        public virtual SubmitResult? SubmitResult { get; set; }
        public virtual User? Mentor { get; set; }
    }
}
