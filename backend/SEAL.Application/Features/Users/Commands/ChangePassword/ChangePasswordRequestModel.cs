namespace SEAL_Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordRequestModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}
