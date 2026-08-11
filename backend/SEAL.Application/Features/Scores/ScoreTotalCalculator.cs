using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEAL_Application.Features.Scores
{
    /// <summary>
    /// Công thức TotalScore DUY NHẤT của hệ thống — HỆ 10 CÓ TRỌNG SỐ, trùng khớp với SaveScore:
    ///   TotalScore = Σ (value / MaxScore × Weight/100) × 10   (tổng Weight trong 1 template = 100%).
    /// Mọi đường ghi điểm (SaveScore, CreateScoreDetail, UpdateScoreDetail, DeleteScoreDetail)
    /// PHẢI dùng chung công thức này — trước đây đường ScoreDetail cộng TỔNG THÔ làm hai phiếu
    /// của hai giám khảo nằm trên hai thang điểm khác nhau và kết quả trung bình bị sai hoàn toàn.
    /// </summary>
    public static class ScoreTotalCalculator
    {
        public static decimal Compute(IEnumerable<ScoreDetail> details, IReadOnlyCollection<TemplateCriteria> templateCriterias)
        {
            decimal weightedTotal = 0m;
            foreach (var d in details)
            {
                var tc = templateCriterias.FirstOrDefault(
                    c => c.TemplateId == d.TemplateId && c.CriteriaId == d.CriteriaId);
                if (tc != null && tc.MaxScore > 0m)
                {
                    weightedTotal += d.Value / tc.MaxScore * (tc.Weight / 100m) * 10m;
                }
            }
            return Math.Round(weightedTotal, 2, MidpointRounding.AwayFromZero);
        }
    }
}
