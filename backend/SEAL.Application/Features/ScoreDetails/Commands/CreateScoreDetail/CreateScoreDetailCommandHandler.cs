using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail
{
    public class CreateScoreDetailCommandHandler : IRequestHandler<CreateScoreDetailCommand, Result<CreateScoreDetailResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public CreateScoreDetailCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<CreateScoreDetailResponseModel>> Handle(CreateScoreDetailCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Score (phiếu chấm) có tồn tại
            var score = await _unitOfWork.GetRepository<Score>().GetByIdAsync(request.Model.ScoreId);
            if (score == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Phiếu chấm có ID '{request.Model.ScoreId}' không tồn tại.");
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
                return new BaseException.ForbiddenException("Bạn không có quyền thêm điểm chi tiết vào phiếu chấm này.");
            }

            // 1c. KHÓA SAU CÔNG BỐ: kết quả vòng đã tính thì điểm chi tiết đóng băng
            //     (thêm điểm làm TotalScore lệch khỏi kết quả đã công bố).
            var lockedSubmit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(score.SubmitResultId);
            if (lockedSubmit != null)
            {
                var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == lockedSubmit.RoundId, cancellationToken);
                if (roundPublished)
                {
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể thêm điểm chi tiết.");
                }
            }

            // 2. Kiểm tra TemplateCriteria có tồn tại (composite key) và lấy MaxScore
            var templateCriteria = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .FirstOrDefaultAsync(tc => tc.TemplateId == request.Model.TemplateId && tc.CriteriaId == request.Model.CriteriaId, cancellationToken);
            if (templateCriteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí (Template '{request.Model.TemplateId}', Criteria '{request.Model.CriteriaId}') không tồn tại.");
            }

            // 2b. Điểm chấm không được vượt quá MaxScore của tiêu chí
            if (request.Model.Value > templateCriteria.MaxScore)
            {
                return BaseException.BadRequestResponse($"Điểm chấm ({request.Model.Value}) vượt quá điểm tối đa của tiêu chí ({templateCriteria.MaxScore}).");
            }

            // 3. Một phiếu chấm không có 2 điểm chi tiết cho cùng một tiêu chí
            var isDuplicate = await _unitOfWork.GetRepository<ScoreDetail>().AnyAsync(
                d => d.ScoreId == request.Model.ScoreId
                  && d.TemplateId == request.Model.TemplateId
                  && d.CriteriaId == request.Model.CriteriaId,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Điểm chi tiết cho tiêu chí này đã tồn tại trong phiếu chấm.");
            }

            // 4. Tạo ScoreDetail
            var scoreDetail = new ScoreDetail
            {
                ScoreId = request.Model.ScoreId,
                TemplateId = request.Model.TemplateId,
                CriteriaId = request.Model.CriteriaId,
                Value = request.Model.Value
            };

            await _unitOfWork.GetRepository<ScoreDetail>().AddAsync(scoreDetail);

            // 5. Auto-update Score.TotalScore theo CÙNG công thức HỆ 10 CÓ TRỌNG SỐ với SaveScore
            //    (trước đây cộng tổng thô -> hai đường chấm ra hai thang điểm khác nhau, kết quả sai).
            var allDetails = await _unitOfWork.GetRepository<ScoreDetail>().Entities
                .AsNoTracking()
                .Where(d => d.ScoreId == request.Model.ScoreId)
                .ToListAsync(cancellationToken);
            allDetails.Add(scoreDetail); // dòng vừa thêm chưa nằm trong DB
            var templateIds = allDetails.Select(d => d.TemplateId).Distinct().ToList();
            var criteriasOfTemplates = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .AsNoTracking()
                .Where(tc => templateIds.Contains(tc.TemplateId))
                .ToListAsync(cancellationToken);
            score.TotalScore = SEAL_Application.Features.Scores.ScoreTotalCalculator.Compute(allDetails, criteriasOfTemplates);
            score.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<Score>().UpdateAsync(score);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateScoreDetailResponseModel
            {
                Id = scoreDetail.Id,
                ScoreId = scoreDetail.ScoreId,
                TemplateId = scoreDetail.TemplateId,
                CriteriaId = scoreDetail.CriteriaId,
                Value = scoreDetail.Value,
                ScoreTotal = score.TotalScore,
                CreatedTime = scoreDetail.CreatedTime
            };
        }
    }
}


