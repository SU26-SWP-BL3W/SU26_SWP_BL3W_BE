namespace SEAL_Domain.Entity.Enums
{
    public enum EventRoleInvitationStatus
    {
        /// <summary>Đã gửi lời mời, đang chờ người được mời phản hồi.</summary>
        Pending = 0,

        /// <summary>Người được mời đã chấp nhận vai trò.</summary>
        Accepted = 1,

        /// <summary>Người được mời đã từ chối lời mời.</summary>
        Rejected = 2,

        /// <summary>Lời mời đã hết hạn (quá thời gian phản hồi).</summary>
        Expired = 3
    }
}
