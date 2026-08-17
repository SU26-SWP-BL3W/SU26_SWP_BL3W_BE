using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Scores.Commands.SaveScore.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Commands.SaveScore
{
    /// <summary>
    /// Gộp Tạo + Cập nhật phiếu chấm (Score) kèm toàn bộ điểm chi tiết (ScoreDetail) trong 1 lần gọi.
    /// Định danh phiếu chấm theo (EventRoleId, SubmitResultId): chưa có -> tạo mới, đã có -> cập nhật.
    /// Danh sách Details là nguồn chuẩn: tiêu chí mới -> thêm, đã có -> sửa điểm, không còn -> xóa.
    /// TotalScore luôn được tính lại = điểm quy về HỆ 10 có TRỌNG SỐ: Σ (value/MaxScore × Weight/100) × 10.
    /// </summary>
    public class SaveScoreCommandHandler : IRequestHandler<SaveScoreCommand, Result<SaveScoreResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;
        private readonly IAuditLogService _auditLogService;

        public SaveScoreCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker,
            IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _auditLogService = auditLogService;
        }

        public async Task<Result<SaveScoreResponseModel>> Handle(SaveScoreCommand request, CancellationToken cancellationToken)
        {
            var m = request.Model;

            // 1. EventRole (giám khảo) phải tồn tại
            var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(m.EventRoleId);
            if (eventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vai trò chấm điểm có ID '{m.EventRoleId}' không tồn tại.");
            }

            // 1a. Phiếu chấm CHỈ hợp lệ khi thuộc vai trò GIÁM KHẢO (Judge). Mentor/EC... không được
            //     chấm điểm (điểm được gộp vào trung bình khi công bố -> tránh sai kết quả).
            if (eventRole.RoleName != SEAL_Domain.Entity.Enums.EventRoleType.Judge)
            {
                return new BaseException.ForbiddenException("Chỉ Giám khảo (Judge) mới được chấm điểm.");
            }

            // 2. Quyền: chỉ chính giám khảo đó hoặc EventCoordinator
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
                return new BaseException.ForbiddenException("Bạn không thể lưu phiếu chấm dưới vai trò của người khác.");
            }

            // 2b. Lấy bài nộp; Giám khảo (Judge) gắn Track chỉ được chấm bài thuộc đúng hạng mục được phân công.
            var submit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(m.SubmitResultId);
            if (submit == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bài nộp '{m.SubmitResultId}' không tồn tại.");
            }

            var scoredTeam = await _unitOfWork.GetRepository<Team>().GetByIdAsync(submit.TeamId);
            if (scoredTeam != null && scoredTeam.Status == SEAL_Domain.Entity.Enums.TeamStatus.Disqualified)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã bị loại nên không thể chấm bài.");
            }
            if (eventRole.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.Judge
                && !string.IsNullOrEmpty(eventRole.TrackId)
                && submit.TrackId != eventRole.TrackId)
            {
                return new BaseException.ForbiddenException("Giám khảo chỉ được chấm bài nộp thuộc hạng mục được phân công.");
            }

            // 2d. Chặn xung đột lợi ích: người chấm KHÔNG được là thành viên của đội có bài nộp này.
            var isMemberOfScoredTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == eventRole.UserId && er.TeamId == submit.TeamId
                   && (er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamLeader
                    || er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamMember),
                cancellationToken);
            if (isMemberOfScoredTeam)
            {
                return new BaseException.ForbiddenException("Không thể chấm bài của đội mà bạn là thành viên (xung đột lợi ích).");
            }

            // 2c. Bộ tiêu chí CHUẨN theo hạng mục của bài nộp (Track -> Template).
            //     Bắt buộc chấm ĐỦ tất cả tiêu chí của bộ (không thiếu, không thừa) để điểm hệ 10 chuẩn xác.
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(submit.TrackId);
            if (track == null || string.IsNullOrEmpty(track.TemplateId))
            {
                return BaseException.BadRequestResponse("Hạng mục của bài nộp chưa gắn bộ tiêu chí (Template).");
            }

            var scoreRepo = _unitOfWork.GetRepository<Score>();
            var score = await scoreRepo.Entities
                .Include(s => s.ScoreDetails)
                .FirstOrDefaultAsync(s => s.EventRoleId == m.EventRoleId && s.SubmitResultId == m.SubmitResultId, cancellationToken);

            bool isNew = score == null;
            bool isAssignedAppeal = await _unitOfWork.GetRepository<Appeal>().AnyAsync(
                a => a.SubmitResultId == m.SubmitResultId 
                  && a.Status == SEAL_Domain.Entity.Enums.AppealStatus.Approved
                  && a.AssignedJudgeId == m.EventRoleId, 
                cancellationToken);

            if (!isNew && score!.IsSubmitted && !isAssignedAppeal)
            {
                return new BaseException.ForbiddenException("Bài thi này đã được chốt điểm và không thể chỉnh sửa trừ khi có yêu cầu phúc khảo được phân công cho bạn.");
            }

            if (!isAssignedAppeal)
            {
                // 2e'. CHỈ ĐƯỢC CHẤM SAU KHI HẠNG MỤC KẾT THÚC NỘP BÀI: trong thời gian hạng mục còn mở, đội vẫn được
                //      sửa bài (chấm sớm sẽ khóa oan quyền sửa của đội vì bài đã-có-điểm không cho sửa).
                var scoringRound = await _unitOfWork.GetRepository<Round>().GetByIdAsync(submit.RoundId);
                var effectiveEndDate = track.EndDate ?? scoringRound?.EndDate;
                if (effectiveEndDate.HasValue && System.DateTime.UtcNow <= effectiveEndDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Hạng mục chưa kết thúc nộp bài nên chưa thể chấm (đội vẫn còn quyền nộp/sửa bài).");
                }

                // 2e''. Cửa sổ chấm điểm (ưu tiên Track, fallback Round): mở tại ScoringStartDate, đóng tại ScoringEndDate.
                var effectiveScoringStartDate = track.ScoringStartDate ?? scoringRound?.ScoringStartDate;
                var effectiveScoringEndDate = track.ScoringEndDate ?? scoringRound?.ScoringEndDate;
                var now = System.DateTime.UtcNow;

                if (effectiveScoringStartDate.HasValue && now < effectiveScoringStartDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Chưa tới thời gian chấm điểm của hạng mục này.");
                }
                if (effectiveScoringEndDate.HasValue && now > effectiveScoringEndDate.Value)
                {
                    return new BaseException.ForbiddenException("Đã hết hạn chấm điểm của hạng mục này.");
                }

                // 2e. Khóa chấm khi kết quả vòng đã được tính/công bố (tránh sửa điểm làm lệch kết quả đã công bố).
                var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == submit.RoundId, cancellationToken);
                if (roundPublished)
                {
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể sửa điểm chấm.");
                }
            }

            var templateCriterias = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .AsNoTracking()
                .Where(tc => tc.TemplateId == track.TemplateId)
                .ToListAsync(cancellationToken);
            if (templateCriterias.Count == 0)
            {
                return BaseException.BadRequestResponse("Bộ tiêu chí của hạng mục chưa cấu hình tiêu chí nào.");
            }
            var requiredKeys = templateCriterias.Select(tc => tc.TemplateId + "|" + tc.CriteriaId).ToHashSet();
            var providedKeys = m.Details.Select(d => d.TemplateId + "|" + d.CriteriaId).ToHashSet();
            if (!requiredKeys.SetEquals(providedKeys))
            {
                return BaseException.BadRequestResponse(
                    $"Phải chấm đúng đủ {requiredKeys.Count} tiêu chí của bộ tiêu chí (không thiếu, không thừa).");
            }

            // 3. Không cho lặp tiêu chí trong cùng một lần gửi
            var duplicateKey = m.Details
                .GroupBy(d => new { d.TemplateId, d.CriteriaId })
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateKey != null)
            {
                return BaseException.BadRequestDupplicationResponse(
                    $"Tiêu chí (Template '{duplicateKey.Key.TemplateId}', Criteria '{duplicateKey.Key.CriteriaId}') bị lặp trong danh sách điểm chi tiết.");
            }

            // 4. Điểm không vượt MaxScore; tính TotalScore quy về HỆ 10 có TRỌNG SỐ:
            //    TotalScore = Σ (value / MaxScore × Weight/100) × 10  (weights trong 1 template cộng lại = 100%).
            //    (Bước 2c đã đảm bảo mọi tiêu chí gửi lên đều thuộc bộ tiêu chí chuẩn.)
            decimal weightedTotal = 0m;
            foreach (var item in m.Details)
            {
                var templateCriteria = templateCriterias
                    .First(tc => tc.TemplateId == item.TemplateId && tc.CriteriaId == item.CriteriaId);
                if (item.Value > templateCriteria.MaxScore)
                {
                    return BaseException.BadRequestResponse($"Điểm chấm ({item.Value}) vượt quá điểm tối đa của tiêu chí ({templateCriteria.MaxScore}).");
                }
                if (templateCriteria.MaxScore > 0m)
                {
                    weightedTotal += item.Value / templateCriteria.MaxScore * (templateCriteria.Weight / 100m) * 10m;
                }
            }

            // 5. Upsert phiếu chấm theo (EventRoleId, SubmitResultId)
            // (Đã lấy score và scoreRepo ở trên)

            if (isNew)
            {
                score = new Score
                {
                    EventRoleId = m.EventRoleId,
                    SubmitResultId = m.SubmitResultId
                };
            }

            score!.Comment = m.Comment;
            score.TotalScore = Math.Round(weightedTotal, 2, MidpointRounding.AwayFromZero);
            score.IsSubmitted = m.IsSubmitted;
            score.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 6. Upsert danh sách điểm chi tiết
            var detailRepo = _unitOfWork.GetRepository<ScoreDetail>();
            var existingDetails = isNew
                ? new List<ScoreDetail>()
                : (score.ScoreDetails?.ToList() ?? new List<ScoreDetail>());

            var resultDetails = new List<SaveScoreDetailResultItem>();
            var toAdd = new List<ScoreDetail>();

            foreach (var item in m.Details)
            {
                var existing = existingDetails.FirstOrDefault(
                    d => d.TemplateId == item.TemplateId && d.CriteriaId == item.CriteriaId);
                if (existing != null)
                {
                    existing.Value = item.Value;
                    resultDetails.Add(new SaveScoreDetailResultItem
                    {
                        Id = existing.Id,
                        TemplateId = existing.TemplateId,
                        CriteriaId = existing.CriteriaId,
                        Value = existing.Value
                    });
                }
                else
                {
                    var newDetail = new ScoreDetail
                    {
                        ScoreId = score.Id,
                        TemplateId = item.TemplateId,
                        CriteriaId = item.CriteriaId,
                        Value = item.Value
                    };
                    toAdd.Add(newDetail);
                    resultDetails.Add(new SaveScoreDetailResultItem
                    {
                        Id = newDetail.Id,
                        TemplateId = newDetail.TemplateId,
                        CriteriaId = newDetail.CriteriaId,
                        Value = newDetail.Value
                    });
                }
            }

            // Xóa các tiêu chí cũ không còn trong danh sách gửi lên
            var requestKeys = m.Details.Select(d => d.TemplateId + "|" + d.CriteriaId).ToHashSet();
            var toRemove = existingDetails
                .Where(d => !requestKeys.Contains(d.TemplateId + "|" + d.CriteriaId))
                .ToList();

            // 7. Lưu thay đổi (Score đã tồn tại được EF tracking theo dõi sẵn)
            if (isNew)
            {
                await scoreRepo.AddAsync(score);
            }
            if (toAdd.Count > 0)
            {
                await detailRepo.AddRangeAsync(toAdd);
            }
            if (toRemove.Count > 0)
            {
                await detailRepo.DeleteRangeAsync(toRemove);
            }

            if (m.IsSubmitted)
            {
                await _auditLogService.AppendAsync(
                    AuditActions.SaveScoreSubmitted,
                    AuditEntityTypes.Score,
                    score.Id,
                    eventRole.EventId,
                    $"Chốt phiếu chấm {score.TotalScore} cho bài {submit.Id}",
                    new { submit.TeamId, submit.TrackId, score.TotalScore },
                    cancellationToken);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (isNew)
            {
                // Race: 2 request SaveScore đồng thời (double-click/2 tab) cùng thấy chưa có
                // Score (isNew=true) -> request kia đã tạo trước, unique index
                // (EventRoleId, SubmitResultId) ở ScoreConfiguration chặn request này. Trả lỗi rõ
                // ràng để client tải lại thay vì crash 500 hoặc âm thầm tạo 2 phiếu chấm trùng.
                return BaseException.BadRequestDupplicationResponse(
                    "Phiếu chấm cho bài nộp này vừa được tạo bởi một yêu cầu khác (gửi trùng lặp). Vui lòng tải lại trang và thử lại.");
            }

            return new SaveScoreResponseModel
            {
                Id = score.Id,
                EventRoleId = score.EventRoleId,
                SubmitResultId = score.SubmitResultId,
                TotalScore = score.TotalScore,
                Comment = score.Comment,
                IsSubmitted = score.IsSubmitted,
                IsNew = isNew,
                Details = resultDetails,
                CreatedTime = score.CreatedTime,
                LastUpdatedTime = score.LastUpdatedTime
            };
        }
    }
}


