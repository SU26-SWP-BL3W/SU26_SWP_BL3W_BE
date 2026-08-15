using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Interfaces
{
    public interface IAuditLogService
    {
        Task AppendAsync(
            string action,
            string entityType,
            string entityId,
            string? eventId,
            string? summary,
            object? payload = null,
            CancellationToken cancellationToken = default);
    }
}
