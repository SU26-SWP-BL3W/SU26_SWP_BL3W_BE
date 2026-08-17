using System;

namespace SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs.Models
{
    public class AuditLogItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string? EventId { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? PayloadJson { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
