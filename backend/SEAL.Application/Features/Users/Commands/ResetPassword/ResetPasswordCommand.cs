using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Users.Commands.ResetPassword
{
    public class ResetPasswordRequestModel
    {
        /// <summary>Token nhận từ email đặt lại mật khẩu.</summary>
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Đặt lại mật khẩu bằng token hợp lệ (còn hạn) từ email quên mật khẩu.</summary>
    public class ResetPasswordCommand : IRequest<Result<bool>>
    {
        public ResetPasswordRequestModel Model { get; set; } = new();
    }
}

