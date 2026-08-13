namespace SEAL_Domain.Entity.Enums
{
    public enum TeamInvitationStatus
    {
        /// <summary>Đã gửi lời mời, đang chờ thành viên phản hồi.</summary>
        PendingAccept,

        /// <summary>
        /// Yêu cầu CHUYỂN QUYỀN Trưởng nhóm cho người này, đang chờ họ chấp nhận.
        /// (Tái dùng bảng TeamInvitations làm thông báo chờ-xác-nhận, tránh tạo bảng thông báo mới.)
        /// InvitedUserId = người được chuyển quyền · InvitedByUserId = Trưởng nhóm hiện tại.
        /// </summary>
        TransferPending,

        /// <summary>Thành viên đã đồng ý vào đội.</summary>
        Accepted,

        /// <summary>Thành viên đã từ chối lời mời.</summary>
        Declined,

        /// <summary>Lời mời hết hạn (quá 24h không phản hồi).</summary>
        Expired
    }
}
