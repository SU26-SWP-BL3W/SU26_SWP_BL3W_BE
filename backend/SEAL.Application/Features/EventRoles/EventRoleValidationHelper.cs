using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles
{
    public static class EventRoleValidationHelper
    {
        /// <summary>
        /// Kiểm tra xung đột vai trò của người dùng trong một sự kiện.
        /// Trả về chuỗi thông báo lỗi nếu có xung đột, ngược lại trả về null.
        /// </summary>
        public static async Task<string?> CheckRoleConflictAsync(
            IUnitOfWork unitOfWork,
            string userId,
            string eventId,
            EventRoleType newRole,
            string? newTrackId,
            string? excludeEventRoleId,
            CancellationToken cancellationToken)
        {
            var query = unitOfWork.GetRepository<EventRole>().GetQueryable()
                .Where(er => er.UserId == userId && er.EventId == eventId);

            if (!string.IsNullOrEmpty(excludeEventRoleId))
            {
                query = query.Where(er => er.Id != excludeEventRoleId);
            }

            var currentRoles = await query.ToListAsync(cancellationToken);

            // 1. Kiểm tra trùng lặp chính xác cùng một vai trò trên cùng một hạng mục (Track)
            if (currentRoles.Any(r => r.RoleName == newRole && r.TrackId == newTrackId))
            {
                return $"Người dùng đã có vai trò '{newRole}' trong sự kiện này.";
            }

            // 2. Kiểm tra xung đột chéo
            if (newRole == EventRoleType.TeamLeader || newRole == EventRoleType.TeamMember)
            {
                if (currentRoles.Any(r => r.RoleName == EventRoleType.EventCoordinator
                                       || r.RoleName == EventRoleType.Judge
                                       || r.RoleName == EventRoleType.Mentor))
                {
                    return "Thí sinh không được phép kiêm nhiệm vai trò Ban tổ chức, Giám khảo hoặc Cố vấn.";
                }
            }
            else
            {
                if (currentRoles.Any(r => r.RoleName == EventRoleType.TeamLeader || r.RoleName == EventRoleType.TeamMember))
                {
                    return $"Người dùng đã tham gia với tư cách Thí sinh trong sự kiện, không thể gán vai trò {newRole}.";
                }
            }

            if (newRole == EventRoleType.EventCoordinator)
            {
                if (currentRoles.Any(r => r.RoleName == EventRoleType.Judge || r.RoleName == EventRoleType.Mentor))
                {
                    return "Người điều phối sự kiện (Event Coordinator) không được kiêm nhiệm vai trò Giám khảo hoặc Cố vấn.";
                }
            }
            else if (newRole == EventRoleType.Judge || newRole == EventRoleType.Mentor)
            {
                if (currentRoles.Any(r => r.RoleName == EventRoleType.EventCoordinator))
                {
                    return $"Người điều phối sự kiện không được kiêm nhiệm vai trò {newRole}.";
                }
            }

            // Giám khảo và Cố vấn (Mentor) LOẠI TRỪ NHAU trong cùng một hạng mục (tránh mọi nghi ngờ thiên vị).
            // Một người có thể vừa làm Giám khảo hạng mục này, vừa làm Cố vấn hạng mục khác trong cùng sự kiện.
            if (newRole == EventRoleType.Judge)
            {
                if (currentRoles.Any(r => r.RoleName == EventRoleType.Mentor && r.TrackId == newTrackId))
                {
                    return "Một người không thể vừa làm Giám khảo vừa làm Cố vấn trong cùng một hạng mục.";
                }
            }
            else if (newRole == EventRoleType.Mentor)
            {
                if (currentRoles.Any(r => r.RoleName == EventRoleType.Judge && r.TrackId == newTrackId))
                {
                    return "Một người không thể vừa làm Cố vấn vừa làm Giám khảo trong cùng một hạng mục.";
                }
            }

            return null;
        }
    }
}
