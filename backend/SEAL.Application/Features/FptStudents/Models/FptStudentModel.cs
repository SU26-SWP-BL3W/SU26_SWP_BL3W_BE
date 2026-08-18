namespace SEAL_Application.Features.FptStudents.Models
{
    public class FptStudentModel
    {
        public bool IsValid { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string Campus { get; set; } = string.Empty;
        public int EnrollYear { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
