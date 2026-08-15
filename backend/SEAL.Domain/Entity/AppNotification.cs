using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class AppNotification : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
        public bool IsRead { get; set; }
        public string? LinkUrl { get; set; }
    }
}
