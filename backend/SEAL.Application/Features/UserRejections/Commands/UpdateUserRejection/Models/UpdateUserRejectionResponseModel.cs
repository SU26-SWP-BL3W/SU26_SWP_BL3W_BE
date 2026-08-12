using System;

namespace SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection.Models
{
    public class UpdateUserRejectionResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RejectedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
