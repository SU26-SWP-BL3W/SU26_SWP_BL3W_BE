using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;

namespace SEAL_Application.Features.Scores
{
    /// <summary>
    /// Chặn điểm âm và bộ tiêu chí lệch 100% — dùng chung SaveScore và CRUD ScoreDetail.
    /// </summary>
    public static class ScoreScoringRules
    {
        public static BaseException.ErrorException? ValidateValue(decimal value, decimal maxScore)
        {
            if (value < 0m)
            {
                return BaseException.BadRequestResponse("Điểm chấm không được âm.");
            }
            if (value > maxScore)
            {
                return BaseException.BadRequestResponse(
                    $"Điểm chấm ({value}) vượt quá điểm tối đa của tiêu chí ({maxScore}).");
            }
            return null;
        }

        public static BaseException.ErrorException? ValidateTemplateWeights(IReadOnlyCollection<TemplateCriteria> criterias)
        {
            var total = criterias.Sum(tc => tc.Weight);
            if (total != 100m)
            {
                return BaseException.BadRequestResponse(
                    $"Tổng trọng số bộ tiêu chí hiện là {total}%, phải đúng 100% mới được chấm.");
            }
            return null;
        }
    }
}
