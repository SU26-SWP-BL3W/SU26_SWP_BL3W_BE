namespace SEAL_Application.Features.UserRejections.Commands.CreateUserRejection.Models
{
    public class CreateUserRejectionRequestModel
    {
        public string UserId { get; set; } = string.Empty;
        public string RejectedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
