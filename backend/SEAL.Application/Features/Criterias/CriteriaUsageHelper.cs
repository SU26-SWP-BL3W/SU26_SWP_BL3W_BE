using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias
{
    internal static class CriteriaUsageHelper
    {
        /// <summary>Tiêu chí đã có ít nhất một dòng điểm chi tiết (đã/đang được chấm).</summary>
        public static Task<bool> IsUsedInScoringAsync(
            IUnitOfWork unitOfWork,
            string criteriaId,
            CancellationToken cancellationToken)
        {
            return unitOfWork.GetRepository<ScoreDetail>().AnyAsync(
                sd => sd.CriteriaId == criteriaId, cancellationToken);
        }
    }
}
