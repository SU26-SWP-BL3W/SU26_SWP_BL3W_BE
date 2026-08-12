namespace SEAL_Application.Features.Users.Commands.RequestUnblock.Models
{
    public class RequestUnblockRequestModel
    {
        /// <summary>Email của tài khoản đang bị khóa muốn xin gỡ khóa.</summary>
        public string Email { get; set; } = string.Empty;
    }
}
