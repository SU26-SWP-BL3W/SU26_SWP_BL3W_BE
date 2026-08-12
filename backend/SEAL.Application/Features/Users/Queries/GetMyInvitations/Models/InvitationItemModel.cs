using System;

namespace SEAL_Application.Features.Users.Queries.GetMyInvitations.Models
{
    public class InvitationItemModel
    {
        public string InvitationId { get; set; } = string.Empty;

        /// <summary>Loại lời mời: TEAM hoặc EVENT_ROLE.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Tên đối tượng: tên đội (TEAM) hoặc tên sự kiện (EVENT_ROLE).</summary>
        public string TargetName { get; set; } = string.Empty;

        /// <summary>Tên người gửi lời mời.</summary>
        public string InviterName { get; set; } = string.Empty;

        /// <summary>Vai trò được mời.</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Tên hạng mục (Track) được mời — chỉ có với Judge/Mentor theo hạng mục; null với EC/đội (toàn sự kiện).</summary>
        public string? TrackName { get; set; }

        /// <summary>Trạng thái đã chuẩn hóa cho FE: PendingAccept (còn nút Đồng ý/Từ chối) | Accepted | Declined.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Thời điểm đã phản hồi (null nếu còn chờ).</summary>
        public DateTime? RespondedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
