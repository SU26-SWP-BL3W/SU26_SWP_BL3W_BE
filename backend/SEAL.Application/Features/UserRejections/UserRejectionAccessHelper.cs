using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections
{
    internal static class UserRejectionAccessHelper
    {
        /// <summary>Admin hoặc EC còn hiệu lực ở bất kỳ sự kiện nào.</summary>
        public static async Task<bool> IsAdminOrActiveCoordinatorAsync(
            IUnitOfWork unitOfWork,
            User? currentUser,
            CancellationToken cancellationToken)
        {
            if (currentUser == null)
            {
                return false;
            }

            if (currentUser.IsAdmin)
            {
                return true;
            }

            var now = DateTime.UtcNow;
            return await unitOfWork.GetRepository<EventRole>().GetQueryable()
                .AsNoTracking()
                .AnyAsync(er => er.UserId == currentUser.Id
                             && er.RoleName == EventRoleType.EventCoordinator
                             && (er.ExpiredAt == null || er.ExpiredAt > now),
                    cancellationToken);
        }

        /// <summary>EC của một sự kiện mà target user có vai trò thí sinh (TeamId != null).</summary>
        public static async Task<bool> IsCoordinatorForUserAsync(
            IUnitOfWork unitOfWork,
            IEventRoleChecker eventRoleChecker,
            string coordinatorUserId,
            string targetUserId,
            CancellationToken cancellationToken)
        {
            var eventIds = await unitOfWork.GetRepository<EventRole>().GetQueryable()
                .AsNoTracking()
                .Where(er => er.UserId == targetUserId && er.TeamId != null)
                .Select(er => er.EventId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var eventId in eventIds)
            {
                if (await eventRoleChecker.HasRoleAsync(
                        coordinatorUserId, eventId, new[] { EventRoleType.EventCoordinator }, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Người tạo bản ghi từ chối (RejectedBy hoặc CreatedBy cho dữ liệu cũ).</summary>
        public static bool IsRejectionOwner(UserRejection rejection, string currentUserId)
        {
            return rejection.RejectedBy == currentUserId
                || rejection.CreatedBy == currentUserId;
        }
    }
}
