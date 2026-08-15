using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(
            string userId,
            string title,
            string message,
            string type = "info",
            string? linkUrl = null,
            CancellationToken cancellationToken = default);

        Task NotifyManyAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            string type = "info",
            string? linkUrl = null,
            CancellationToken cancellationToken = default);
    }
}
