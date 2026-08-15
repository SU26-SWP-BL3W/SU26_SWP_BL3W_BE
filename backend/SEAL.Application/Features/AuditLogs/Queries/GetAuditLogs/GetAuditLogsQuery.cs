using SEAL_Application.Commons;
using SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs.Models;
using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsQuery : BasePaginationQuery, IRequest<Result<PagedResult<AuditLogItemModel>>>
    {
        public string EventId { get; set; } = string.Empty;
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }

        public override System.Collections.Generic.HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase)
        {
            "CreatedTime",
            "Action",
            "EntityType"
        };
    }
}
