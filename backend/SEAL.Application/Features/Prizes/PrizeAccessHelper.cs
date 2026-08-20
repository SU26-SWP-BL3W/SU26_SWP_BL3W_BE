using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Prizes
{
    internal static class PrizeAccessHelper
    {
        /// <summary>Admin hoặc EC còn hiệu lực của đúng sự kiện chứa giải. Null = OK.</summary>
        public static async Task<BaseException.ErrorException?> EnsureCanManageEventPrizesAsync(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            string eventId,
            CancellationToken cancellationToken)
        {
            var currentUserId = currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return BaseException.UnauthorizedUnAuthorizedResponse();
            }

            var currentUser = await unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (currentUser != null && currentUser.IsAdmin)
            {
                return null;
            }

            var isCoordinator = await eventRoleChecker.HasRoleAsync(
                currentUserId, eventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isCoordinator)
            {
                return BaseException.ForbiddenResponse(
                    "Chỉ Admin hoặc Điều phối viên (EC) của sự kiện này mới được quản lý giải thưởng.");
            }

            return null;
        }
    }
}
