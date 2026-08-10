// [FLOW3-DOITHI][Entity] TeamInvitation: lời mời tham gia đội gửi cho 1 sinh viên.

using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Domain.Entity
{
    public class TeamInvitation : BaseEntity
    {
        public string TeamId { get; set; } = string.Empty;

        /// <summary>User được mời vào đội.</summary>
        public string InvitedUserId { get; set; } = string.Empty;

        /// <summary>User (TeamLeader) đã gửi lời mời.</summary>
        public string InvitedByUserId { get; set; } = string.Empty;

        public TeamInvitationStatus Status { get; set; } = TeamInvitationStatus.PendingAccept;

        /// <summary>Thời điểm hết hạn lời mời (mặc định 48h sau khi tạo).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Thời điểm thành viên phản hồi (accept/decline).</summary>
        public DateTime? RespondedAt { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties
        public virtual Team Team { get; set; } = null!;
        public virtual User InvitedUser { get; set; } = null!;
    }
}
