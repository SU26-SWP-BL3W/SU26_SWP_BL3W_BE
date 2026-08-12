namespace SEAL_Application.Features.Users.Commands.CreateUser.Models
{
    public class CreateUserRequestModel
    {
        public string SchoolId { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsStudent { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsFpt { get; set; } = true;
        public string? PhotoStudentCardUrl { get; set; }
    }
}
