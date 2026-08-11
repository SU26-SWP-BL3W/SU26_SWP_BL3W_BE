using System;

namespace SEAL_Application.Features.Teams.Commands.InviteTeamMember.Models
{
    public class InviteTeamMemberResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string InvitedUserId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        // true khi người được mời CHƯA có tài khoản: hệ thống vừa tạo tài khoản tạm + gửi mail kích hoạt.
        // FE dùng cờ này để hiện thông báo riêng (họ cần kích hoạt + cập nhật hồ sơ trước khi chấp nhận).
        public bool IsNewTemporaryUser { get; set; }
    }
}
