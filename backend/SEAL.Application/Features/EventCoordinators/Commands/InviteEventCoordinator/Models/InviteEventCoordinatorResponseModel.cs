using System;

namespace SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator.Models
{
    public class InviteEventCoordinatorResponseModel
    {
        public string InvitationId { get; set; } = string.Empty;
        public string InvitedUserId { get; set; } = string.Empty;
        public string CoordinatorEmail { get; set; } = string.Empty;
        public string CoordinatorFullName { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        // Trạng thái lời mời (Pending) — vai trò chỉ được tạo khi người nhận chấp nhận.
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        // true: đã gửi email mời (kèm 2 nút Đồng ý/Từ chối); false: gửi email lời mời lỗi (lời mời vẫn được tạo).
        public bool InvitationEmailSent { get; set; }
        // true: đã gửi email kích hoạt tài khoản tạm; false: email kích hoạt lỗi (tài khoản tạm vẫn được tạo).
        public bool ActivationEmailSent { get; set; }
    }
}
