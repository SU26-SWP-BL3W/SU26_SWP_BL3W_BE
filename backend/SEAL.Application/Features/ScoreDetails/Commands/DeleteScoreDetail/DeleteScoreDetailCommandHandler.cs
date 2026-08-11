using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.ScoreDetails.Commands.DeleteScoreDetail
{
    public class DeleteScoreDetailCommandHandler : IRequestHandler<DeleteScoreDetailCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteScoreDetailCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteScoreDetailCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm ScoreDetail cần xóa
            var scoreDetail = await _unitOfWork.GetRepository<ScoreDetail>().GetByIdAsync(request.Id);
            if (scoreDetail == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Điểm chi tiết có ID '{request.Id}' không tồn tại.");
            }

            // Lấy Score cha để check ownership
            var score = await _unitOfWork.GetRepository<Score>().GetByIdAsync(scoreDetail.ScoreId);
            if (score == null)
            {
                return BaseException.BadRequestNotFoundResponse("Phiếu chấm cha của điểm chi tiết này không tồn tại.");
            }

            // Lấy EventRole của phiếu chấm
            var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(score.EventRoleId);
            if (eventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse("Vai trò chấm điểm của phiếu chấm này không hợp lệ.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isOwnRole = eventRole.UserId == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                eventRole.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isOwnRole && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa điểm chi tiết của phiếu chấm này.");
            }

            // 1b. KHÓA SAU CÔNG BỐ: kết quả vòng đã tính thì điểm chi tiết đóng băng
            //     (xóa điểm làm TotalScore lệch khỏi kết quả đã công bố).
            var lockedSubmit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(score.SubmitResultId);
            if (lockedSubmit != null)
            {
                var lockedTrack = await _unitOfWork.GetRepository<Track>().GetByIdAsync(lockedSubmit.TrackId);
                if (lockedTrack != null)
                {
                    var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                        fr => fr.RoundId == lockedTrack.RoundId, cancellationToken);
                    if (roundPublished)
                    {
                        return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể xóa điểm chi tiết.");
                    }
                }
            }

            var parentScoreId = scoreDetail.ScoreId;

            // 2. Xóa cứng
            await _unitOfWork.GetRepository<ScoreDetail>().DeleteAsync(scoreDetail);

            // 3. Cập nhật lại TotalScore của Score cha theo CÙNG công thức HỆ 10 CÓ TRỌNG SỐ với SaveScore
            //    (trước đây cộng tổng thô -> hai đường chấm ra hai thang điểm khác nhau, kết quả sai).
            if (score != null)
            {
                var remainingDetails = await _unitOfWork.GetRepository<ScoreDetail>().Entities
                    .AsNoTracking()
                    .Where(d => d.ScoreId == parentScoreId && d.Id != scoreDetail.Id)
                    .ToListAsync(cancellationToken);
                var templateIds = remainingDetails.Select(d => d.TemplateId).Distinct().ToList();
                var criteriasOfTemplates = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                    .AsNoTracking()
                    .Where(tc => templateIds.Contains(tc.TemplateId))
                    .ToListAsync(cancellationToken);
                score.TotalScore = SEAL_Application.Features.Scores.ScoreTotalCalculator.Compute(remainingDetails, criteriasOfTemplates);
                score.LastUpdatedTime = CoreHelper.SystemTimeNow;
                await _unitOfWork.GetRepository<Score>().UpdateAsync(score);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}


