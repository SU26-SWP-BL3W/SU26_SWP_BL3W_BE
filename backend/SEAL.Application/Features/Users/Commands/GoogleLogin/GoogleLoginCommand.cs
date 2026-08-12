using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.LoginUser.Models;

namespace SEAL_Application.Features.Users.Commands.GoogleLogin
{
    /// <summary>
    /// Body FE gửi lên: idToken lấy từ nút "Sign in with Google" (Google Identity Services).
    /// </summary>
    public class GoogleLoginRequestModel
    {
        public string IdToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Đăng nhập bằng Google. Trả về cùng khung với đăng nhập thường (AccessToken/RefreshToken).
    /// </summary>
    public class GoogleLoginCommand : IRequest<Result<LoginUserResponseModel>>
    {
        public GoogleLoginRequestModel Model { get; set; } = new GoogleLoginRequestModel();
    }
}

