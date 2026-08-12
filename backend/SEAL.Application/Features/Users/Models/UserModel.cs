using System;

namespace SEAL_Application.Features.Users.Models
{
    public class UserModel
    {
        public string Id { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsStudent { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsApproved { get; set; }
        public bool IsFpt { get; set; }
        public bool IsRejected { get; set; }
        /// <summary>
        /// True = tài khoản tạm được MỜI (giám khảo/mentor), không phải thí sinh đăng ký.
        /// FE dùng để phân biệt: badge "Được mời" thay vì "Chờ duyệt" và ẩn nút Duyệt/Từ chối.
        /// </summary>
        public bool IsTemporary { get; set; }
        public string? PhotoStudentCardUrl { get; set; }
    }
}
