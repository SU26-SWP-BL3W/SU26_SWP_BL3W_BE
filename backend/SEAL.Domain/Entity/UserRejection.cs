using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class UserRejection : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string RejectedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
    }
}
