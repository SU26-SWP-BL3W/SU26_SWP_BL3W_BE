using SEAL_Domain.Entity.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Interfaces
{
    public interface IEventRoleChecker
    {
        Task<bool> HasRoleAsync(string userId, string eventId, EventRoleType[] allowedRoles, CancellationToken cancellationToken = default, string? trackId = null, string? teamId = null);
        void InvalidateCache(string userId, string eventId);
    }
}
