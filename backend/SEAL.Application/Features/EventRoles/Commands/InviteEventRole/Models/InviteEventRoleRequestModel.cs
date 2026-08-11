using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Features.EventRoles.Commands.InviteEventRole.Models
{
    public class InviteEventRoleRequestModel
    {
        /// <summary>Sự kiện mời tham gia.</summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>Người dùng được mời.</summary>
        public string InvitedUserId { get; set; } = string.Empty;

        /// <summary>Vai trò được mời: Judge, Mentor hoặc EventCoordinator.</summary>
        public EventRoleType RoleName { get; set; }

        /// <summary>Track cụ thể (tùy chọn) khi mời Judge/Mentor cho một hạng mục.</summary>
        public string? TrackId { get; set; }

        public string? Notes { get; set; }
    }
}
