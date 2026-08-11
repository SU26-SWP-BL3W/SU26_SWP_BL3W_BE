namespace SEAL_Application.Features.Users.Commands.CreateUser.Models
{
    public class FptStudentResponse
    {
        public bool IsValid { get; set; }
        public FptStudentData? Data { get; set; }
        public string? Message { get; set; }
    }
}
