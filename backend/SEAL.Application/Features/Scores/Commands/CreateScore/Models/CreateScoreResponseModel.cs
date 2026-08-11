using System;

namespace SEAL_Application.Features.Scores.Commands.CreateScore.Models
{
    public class CreateScoreResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
