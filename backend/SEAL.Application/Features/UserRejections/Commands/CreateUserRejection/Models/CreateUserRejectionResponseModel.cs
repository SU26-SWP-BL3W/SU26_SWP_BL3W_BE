using System;

namespace SEAL_Application.Features.UserRejections.Commands.CreateUserRejection.Models
{
    public class CreateUserRejectionResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RejectedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
