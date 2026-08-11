using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Scores.Queries.GetScoreDetail.Models
{
    /// <summary>
    /// Xem chi tiết phiếu chấm kèm toàn bộ điểm chi tiết theo từng tiêu chí (gộp Score + ScoreDetail).
    /// </summary>
    public class ScoreWithDetailsModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? Comment { get; set; }
        public bool IsSubmitted { get; set; }
        public List<ScoreDetailLine> Details { get; set; } = new();
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }

    public class ScoreDetailLine
    {
        public string Id { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
