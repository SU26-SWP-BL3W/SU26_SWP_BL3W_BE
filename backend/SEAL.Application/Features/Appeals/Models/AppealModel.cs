using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Application.Features.Appeals.Models
{
    public class AppealModel
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Response { get; set; }
        public AppealStatus Status { get; set; }
        public string? AssignedJudgeId { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
