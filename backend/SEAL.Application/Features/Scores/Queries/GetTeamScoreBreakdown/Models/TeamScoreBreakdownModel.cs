using System.Collections.Generic;

namespace SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown.Models
{
    public class TeamScoreBreakdownModel
    {
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public List<SubmissionScoreBreakdown> Submissions { get; set; } = new();
    }

    public class SubmissionScoreBreakdown
    {
        public string SubmitResultId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public string RoundId { get; set; } = string.Empty;
        public string RoundName { get; set; } = string.Empty;
        /// <summary>Vòng đã tính/công bố kết quả hay chưa (để FE biết điểm đã chốt).</summary>
        public bool RoundPublished { get; set; }
        public List<JudgeScoreBreakdown> JudgeScores { get; set; } = new();
    }

    public class JudgeScoreBreakdown
    {
        public string JudgeName { get; set; } = string.Empty;
        /// <summary>Tổng điểm quy về hệ 10 có trọng số của giám khảo này.</summary>
        public decimal TotalScore { get; set; }
        public string? Comment { get; set; }
        public bool IsSubmitted { get; set; }
        public SEAL_Domain.Entity.Enums.ScoreType ScoreType { get; set; } = SEAL_Domain.Entity.Enums.ScoreType.Original;
        public string? AppealId { get; set; }
        public List<CriterionScoreLine> Criteria { get; set; } = new();
    }

    public class CriterionScoreLine
    {
        public string CriteriaName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
    }
}
