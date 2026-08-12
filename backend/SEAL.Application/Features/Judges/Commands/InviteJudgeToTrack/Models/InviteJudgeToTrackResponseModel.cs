using System;

namespace SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack.Models
{
    public class InviteJudgeToTrackResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string InvitedUserId { get; set; } = string.Empty;
        public string JudgeEmail { get; set; } = string.Empty;
        public string JudgeFullName { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        // Trạng thái lời mời (Pending) — vai trò chỉ được tạo khi người nhận chấp nhận.
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        // true: đã gửi email mời (kèm 2 nút Đồng ý/Từ chối); false: gửi email lỗi (lời mời vẫn được tạo).
        public bool InvitationEmailSent { get; set; }
    }
}
