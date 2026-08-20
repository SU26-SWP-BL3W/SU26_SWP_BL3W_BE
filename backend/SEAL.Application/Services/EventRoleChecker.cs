using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Services
{
    public class UserEventRoleDto
    {
        public EventRoleType RoleName { get; set; }
        public string? TrackId { get; set; }
        public string? TeamId { get; set; }
    }

    public class EventRoleChecker : IEventRoleChecker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;

        public EventRoleChecker(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        public async Task<bool> HasRoleAsync(string userId, string eventId, EventRoleType[] allowedRoles, CancellationToken cancellationToken = default, string? trackId = null, string? teamId = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(eventId))
            {
                return false;
            }

            var cacheKey = $"EventRole_{userId}_{eventId}";

            // Global Admin Bypass
            var user = await _unitOfWork.GetRepository<User>().GetQueryable()
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
            
            if (user != null && user.IsAdmin)
            {
                return true;
            }

            // Lookup in cache
            if (!_memoryCache.TryGetValue(cacheKey, out UserEventRoleDto[]? userRoles))
            {
            // Fetch from DB — ExpiredAt null → dùng Event.EndDate (role không "sống vĩnh viễn")
                var nowUtc = DateTime.UtcNow;
                userRoles = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                    .AsNoTracking()
                    .Include(er => er.Event)
                    .Where(er => er.UserId == userId && er.EventId == eventId)
                    .Where(er => (er.ExpiredAt ?? (er.Event != null ? (DateTime?)er.Event.EndDate : DateTime.MaxValue)) > nowUtc)
                    .Select(er => new UserEventRoleDto
                    {
                        RoleName = er.RoleName,
                        TrackId = er.TrackId,
                        TeamId = er.TeamId
                    })
                    .ToArrayAsync(cancellationToken);

                // Set to cache with 60s TTL
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
                
                _memoryCache.Set(cacheKey, userRoles, cacheOptions);
            }

            if (userRoles == null || !userRoles.Any())
            {
                return false;
            }

            // 1. Kiểm tra quyền cấp Event (không gán cho Track hay Team cụ thể nào)
            // Ví dụ: EventCoordinator thường có quyền trên toàn bộ Event.
            var hasEventLevelRole = userRoles.Any(ur => 
                allowedRoles.Contains(ur.RoleName) && 
                string.IsNullOrEmpty(ur.TrackId) && 
                string.IsNullOrEmpty(ur.TeamId));

            if (hasEventLevelRole)
            {
                return true;
            }

            // 2. Check quyền cấp Track (nếu có trackId đầu vào)
            if (!string.IsNullOrEmpty(trackId))
            {
                var hasTrackLevelRole = userRoles.Any(ur => 
                    allowedRoles.Contains(ur.RoleName) && ur.TrackId == trackId);

                if (hasTrackLevelRole)
                {
                    return true;
                }
            }

            // 3. Check quyền cấp Team (nếu có teamId đầu vào)
            if (!string.IsNullOrEmpty(teamId))
            {
                var hasTeamLevelRole = userRoles.Any(ur => 
                    allowedRoles.Contains(ur.RoleName) && ur.TeamId == teamId);

                if (hasTeamLevelRole)
                {
                    return true;
                }
            }

            return false;
        }

        public void InvalidateCache(string userId, string eventId)
        {
            var cacheKey = $"EventRole_{userId}_{eventId}";
            _memoryCache.Remove(cacheKey);
        }
    }
}
