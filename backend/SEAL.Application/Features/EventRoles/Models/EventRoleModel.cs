using System;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.EventRoles.Models
{
    public class EventRoleModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string? TeamId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Thông tin hiển thị cho rõ: tên Sự kiện + tên chỗ được gán (Track / Team)
        public string? EventName { get; set; }
        public string? TrackName { get; set; }
        public string? TeamName { get; set; }

        public UserModel? User { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        /// <summary>Ngày kết thúc sự kiện — dùng làm hạn role khi ExpiredAt null.</summary>
        public DateTime? EventEndDate { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
