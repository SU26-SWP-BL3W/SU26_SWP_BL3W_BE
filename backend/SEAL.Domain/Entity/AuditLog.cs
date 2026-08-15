// Chi ghi append-only: Unpublish/tinh lai xoa FinalResult nen can dau vet rieng.

using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class AuditLog : BaseEntity
    {
        public string? EventId { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? PayloadJson { get; set; }
    }
}
