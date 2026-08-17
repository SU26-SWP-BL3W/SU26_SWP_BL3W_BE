namespace SEAL_Application.Features.Users.Commands.LoginUser.Models
{
    public class LoginUserResponseModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsStudent { get; set; }
        public bool MustChangePassword { get; set; }
    }
}
