using SEAL_Domain.Base;
using System.Collections.Generic;
using System;

namespace SEAL_Domain.Entity
{
    public class User : BaseEntity
    {
        public string? SchoolId { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsStudent { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsApproved { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public bool IsEmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationExpiry { get; set; }

        public bool IsFpt { get; set; } = true;
        public string? PhotoStudentCardUrl { get; set; }

        // Tài khoản tạm (dành cho giám khảo được mời chấm theo từng sự kiện)
        public bool IsTemporary { get; set; } = false;
        // Bắt đổi mật khẩu ngay lần đăng nhập đầu tiên — bật khi cấp mật khẩu tạm, tắt sau khi đổi thành công
        public bool MustChangePassword { get; set; } = false;
        // Navigation Properties
        public virtual School? School { get; set; }
        public virtual ICollection<UserRejection> UserRejections { get; set; } = new List<UserRejection>();
        public virtual ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();
    }
}
