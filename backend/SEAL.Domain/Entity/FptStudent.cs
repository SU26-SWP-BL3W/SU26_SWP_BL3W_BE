using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    /// <summary>
    /// Danh sách sinh viên FPT dùng để xác thực hồ sơ (Student Verification) khi
    /// sinh viên hoàn tất onboarding. Bản ghi thật trong DB — Coordinator/Admin quản
    /// lý qua API, không còn phụ thuộc Google Sheet ngoài như bản mock trước đây.
    /// </summary>
    public class FptStudent : BaseEntity
    {
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string Campus { get; set; } = string.Empty;
        public int EnrollYear { get; set; }

        /// <summary>ACTIVE/GRADUATED/SUSPENDED...</summary>
        public string Status { get; set; } = "ACTIVE";
    }
}
