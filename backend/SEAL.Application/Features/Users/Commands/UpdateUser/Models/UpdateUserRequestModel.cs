namespace SEAL_Application.Features.Users.Commands.UpdateUser.Models
{
    public class UpdateUserRequestModel
    {
        public string SchoolId { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool IsStudent { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsApproved { get; set; }
        public bool IsFpt { get; set; }
        public string? PhotoStudentCardUrl { get; set; }
    }
}
