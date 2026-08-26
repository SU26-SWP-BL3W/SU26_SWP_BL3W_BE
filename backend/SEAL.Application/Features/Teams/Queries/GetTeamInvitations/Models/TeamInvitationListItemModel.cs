using System;

namespace SEAL_Application.Features.Teams.Queries.GetTeamInvitations.Models
{
    public class TeamInvitationListItemModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string InvitedUserId { get; set; } = string.Empty;
        public string InvitedUserFullName { get; set; } = string.Empty;
        public string InvitedUserEmail { get; set; } = string.Empty;
        public string? InvitedUserStudentCode { get; set; }
        public string InvitedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// PendingAccept | Accepted | Declined | Expired. Nếu lời mời còn PendingAccept
        /// nhưng đã quá ExpiresAt thì trả về Expired cho khớp thực tế.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Nhãn hiển thị tiếng Việt cho FE bind thẳng:
        /// PendingAccept = "Đang chờ xác nhận" (KHÔNG phải "đang mời" — lời mời đã gửi,
        /// đang chờ người nhận phản hồi) | Accepted = "Đã tham gia" | Declined = "Đã từ chối" | Expired = "Hết hạn".
        /// </summary>
        public string StatusLabel { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// True nếu đây là yêu cầu CHUYỂN QUYỀN Trưởng nhóm (không phải lời mời gia nhập đội
        /// thường) — để FE tách 2 danh sách riêng thay vì tự đoán qua chuỗi Notes.
        /// </summary>
        public bool IsTransfer { get; set; }
    }
}
