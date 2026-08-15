using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail
{
    public class UpdateScoreDetailCommandHandler : IRequestHandler<UpdateScoreDetailCommand, Result<UpdateScoreDetailResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public UpdateScoreDetailCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<UpdateScoreDetailResponseModel>> Handle(UpdateScoreDetailCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm ScoreDetail
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

            bool isOwnRole = eventRole.UserId == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                eventRole.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isOwnRole && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền cập nhật điểm chi tiết của phiếu chấm này.");
            }

            // 1b. KHÓA SAU CÔNG BỐ: kết quả vòng đã tính thì điểm chi tiết đóng băng
            //     (sửa điểm làm TotalScore lệch khỏi kết quả đã công bố).
            var lockedSubmit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(score.SubmitResultId);
            if (lockedSubmit != null)
            {
                var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == lockedSubmit.RoundId, cancellationToken);
                if (roundPublished)
                {
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể sửa điểm chi tiết.");
                }
            }

            // 2. Lấy MaxScore của tiêu chí gốc để kiểm tra điểm chấm không vượt quá
            var templateCriteria = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .FirstOrDefaultAsync(tc => tc.TemplateId == scoreDetail.TemplateId && tc.CriteriaId == scoreDetail.CriteriaId, cancellationToken);
            if (templateCriteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí gốc (Template '{scoreDetail.TemplateId}', Criteria '{scoreDetail.CriteriaId}') không còn tồn tại.");
            }

            if (request.Model.Value > templateCriteria.MaxScore)
            {
                return BaseException.BadRequestResponse($"Điểm chấm ({request.Model.Value}) vượt quá điểm tối đa của tiêu chí ({templateCriteria.MaxScore}).");
            }

            // 3. Cập nhật Value (không cho đổi ScoreId/TemplateId/CriteriaId — muốn đổi thì xóa và tạo lại)
            scoreDetail.Value = request.Model.Value;
            scoreDetail.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<ScoreDetail>().UpdateAsync(scoreDetail);

            // 3. Cập nhật TotalScore của Score cha theo CÙNG công thức HỆ 10 CÓ TRỌNG SỐ với SaveScore
            //    (trước đây cộng tổng thô -> hai đường chấm ra hai thang điểm khác nhau, kết quả sai).
            if (score != null)
            {
                var allDetails = await _unitOfWork.GetRepository<ScoreDetail>().Entities
                    .AsNoTracking()
                    .Where(d => d.ScoreId == scoreDetail.ScoreId)
                    .ToListAsync(cancellationToken);
                // Thay giá trị dòng vừa sửa (DB chưa lưu giá trị mới)
                var edited = allDetails.First(d => d.Id == scoreDetail.Id);
                edited.Value = request.Model.Value;
                var templateIds = allDetails.Select(d => d.TemplateId).Distinct().ToList();
                var criteriasOfTemplates = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                    .AsNoTracking()
                    .Where(tc => templateIds.Contains(tc.TemplateId))
                    .ToListAsync(cancellationToken);
                score.TotalScore = SEAL_Application.Features.Scores.ScoreTotalCalculator.Compute(allDetails, criteriasOfTemplates);
                score.LastUpdatedTime = CoreHelper.SystemTimeNow;
                await _unitOfWork.GetRepository<Score>().UpdateAsync(score);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateScoreDetailResponseModel
            {
                Id = scoreDetail.Id,
                ScoreId = scoreDetail.ScoreId,
                TemplateId = scoreDetail.TemplateId,
                CriteriaId = scoreDetail.CriteriaId,
                Value = scoreDetail.Value,
                ScoreTotal = score?.TotalScore ?? 0,
                LastUpdatedTime = scoreDetail.LastUpdatedTime
            };
        }
    }
}


