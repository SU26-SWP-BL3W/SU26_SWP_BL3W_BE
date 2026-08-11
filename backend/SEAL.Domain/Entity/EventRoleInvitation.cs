using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Domain.Entity
{
    /// <summary>
    /// Lời mời một người dùng đảm nhận vai trò sự kiện (Judge, Mentor, EventCoordinator).
    /// Tách trạng thái "đang chờ" (Pending) khỏi bảng EventRole chính thức.
    /// </summary>
    public class EventRoleInvitation : BaseEntity
    {
        public string EventId { get; set; } = string.Empty;

        /// <summary>Vai trò được mời: Judge, Mentor hoặc EventCoordinator.</summary>
        public EventRoleType RoleName { get; set; }

        /// <summary>Track cụ thể (nếu mời Mentor/Judge cho một hạng mục).</summary>
        public string? TrackId { get; set; }

        /// <summary>Người được mời.</summary>
        public string InvitedUserId { get; set; } = string.Empty;

        /// <summary>Người gửi lời mời (Admin hoặc EventCoordinator).</summary>
        public string InvitedByUserId { get; set; } = string.Empty;

        public EventRoleInvitationStatus Status { get; set; } = EventRoleInvitationStatus.Pending;

        /// <summary>Thời điểm hết hạn lời mời (mặc định 7 ngày sau khi tạo).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Thời điểm người được mời phản hồi (accept/reject).</summary>
        public DateTime? RespondedAt { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties
        public virtual Event Event { get; set; } = null!;
        public virtual User InvitedUser { get; set; } = null!;
        public virtual User InvitedByUser { get; set; } = null!;
        public virtual Track? Track { get; set; }
    }
}
