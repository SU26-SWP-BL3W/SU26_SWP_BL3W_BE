namespace SEAL_Application.Features.Users.Commands.RefreshToken.Models
{
    public class RefreshTokenResponseModel
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
