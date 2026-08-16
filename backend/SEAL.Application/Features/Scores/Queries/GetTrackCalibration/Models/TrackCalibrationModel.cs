using System.Collections.Generic;
using System.Linq;

namespace SEAL_Application.Features.Scores.Queries.GetTrackCalibration.Models
{
    public class TrackCalibrationModel
    {
        public string TrackId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool HasHighVarianceAlert => CriteriaStats.Any(c => c.HasHighVariance);
        public List<CalibrationScoreRow> Scores { get; set; } = new();
        public List<CalibrationCriteriaStat> CriteriaStats { get; set; } = new();
        public List<CalibrationJudgeStat> JudgeStats { get; set; } = new();
    }

    public class CalibrationScoreRow
    {
        public string JudgeId { get; set; } = string.Empty;
        public string JudgeName { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public bool IsSubmitted { get; set; }
        public bool IsAccepted { get; set; }
    }

    public class CalibrationCriteriaStat
    {
        public string CriteriaId { get; set; } = string.Empty;
        public string CriteriaName { get; set; } = string.Empty;
        public decimal Mean { get; set; }
        public decimal StdDev { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal MaxDelta => Max - Min;
        public bool HasHighVariance => (Max - Min) > 2.0m;
        public int SampleCount { get; set; }
    }

    public class CalibrationJudgeStat
    {
        public string JudgeId { get; set; } = string.Empty;
        public string JudgeName { get; set; } = string.Empty;
        public decimal Mean { get; set; }
        public decimal StdDev { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public int SampleCount { get; set; }
    }
}
