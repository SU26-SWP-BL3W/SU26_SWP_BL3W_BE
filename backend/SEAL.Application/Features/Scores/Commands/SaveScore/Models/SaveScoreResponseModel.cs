using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Scores.Commands.SaveScore.Models
{
    public class SaveScoreResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? Comment { get; set; }
        public bool IsSubmitted { get; set; }
        public bool IsNew { get; set; }
        public List<SaveScoreDetailResultItem> Details { get; set; } = new();
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }

    public class SaveScoreDetailResultItem
    {
        public string Id { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
