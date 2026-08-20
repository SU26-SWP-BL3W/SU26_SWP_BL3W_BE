using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.ScoreDetails
{
    internal static class ScoreDetailMutationGuard
    {
        /// <summary>
        /// Khóa sửa/xóa/thêm điểm chi tiết khi phiếu đã chốt (IsSubmitted),
        /// trừ khi có đơn phúc khảo Approved gán đúng giám khảo của phiếu.
        /// Đồng bộ với SaveScore. Null = được phép sửa.
        /// </summary>
        public static async Task<BaseException.ErrorException?> EnsureScoreMutableAsync(
            IUnitOfWork unitOfWork,
            Score score,
            CancellationToken cancellationToken)
        {
            if (!score.IsSubmitted)
            {
                return null;
            }

            var isAssignedAppeal = await unitOfWork.GetRepository<Appeal>().AnyAsync(
                a => a.SubmitResultId == score.SubmitResultId
                  && a.Status == AppealStatus.Approved
                  && a.AssignedJudgeId == score.EventRoleId,
                cancellationToken);

            if (isAssignedAppeal)
            {
                return null;
            }

            return BaseException.ForbiddenResponse(
                "Phiếu chấm đã chốt điểm nên không thể sửa điểm chi tiết trừ khi có phúc khảo được phân công.");
        }
    }
}
