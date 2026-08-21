using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Scores.Models
{
    public class ScoreModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? Comment { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        // Điểm chi tiết từng tiêu chí — để FE nạp lại phiếu nháp khi giám khảo quay lại bài.
        public List<ScoreDetailItemModel> Details { get; set; } = new();
    }

    public class ScoreDetailItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
