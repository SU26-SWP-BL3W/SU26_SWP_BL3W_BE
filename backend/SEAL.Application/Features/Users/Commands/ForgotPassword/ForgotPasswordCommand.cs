using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Users.Commands.ForgotPassword
{
    public class ForgotPasswordRequestModel
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponseModel
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Người dùng quên mật khẩu: gửi email chứa liên kết đặt lại mật khẩu.
    /// Luôn trả về cùng một thông báo chung (chống dò email tồn tại).
    /// </summary>
    public class ForgotPasswordCommand : IRequest<Result<ForgotPasswordResponseModel>>
    {
        public ForgotPasswordRequestModel Model { get; set; } = new();
    }
}

