using System;

namespace SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models
{
    public class UserRejectionModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RejectedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
