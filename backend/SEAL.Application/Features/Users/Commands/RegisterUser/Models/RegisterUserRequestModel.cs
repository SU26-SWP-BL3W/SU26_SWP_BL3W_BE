namespace SEAL_Application.Features.Users.Commands.RegisterUser.Models
{
    public class RegisterUserRequestModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
