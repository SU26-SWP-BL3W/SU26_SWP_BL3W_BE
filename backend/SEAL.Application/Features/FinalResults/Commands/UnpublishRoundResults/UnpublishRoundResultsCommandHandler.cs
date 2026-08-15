using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.UnpublishRoundResults
{
    /// <summary>
    /// HỦY CÔNG BỐ kết quả vòng thi: xóa TRỌN BỘ FinalResult của vòng (nguyên tử, không xóa lẻ dòng).
    /// Dùng khi phát hiện sai sót sau công bố: hủy công bố -> khóa chấm/sửa điểm được mở lại
    /// -> EC/giám khảo sửa -> tính lại bằng CalculateRoundResults.
    /// Chỉ được hủy khi vòng SAU chưa vận hành (chưa có bài nộp/kết quả dựa trên kết quả này).
    /// </summary>
    public class UnpublishRoundResultsCommandHandler : IRequestHandler<UnpublishRoundResultsCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IAuditLogService _auditLogService;

        public UnpublishRoundResultsCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _auditLogService = auditLogService;
        }

        public async Task<Result<bool>> Handle(UnpublishRoundResultsCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Round tồn tại
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.RoundId);
            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.RoundId}' không tồn tại.");
            }

            // 2. Quyền: chỉ EventCoordinator (hoặc Admin) — đối xứng với CalculateRoundResults
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, round.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ EventCoordinator được hủy công bố kết quả vòng thi.");
            }

            // 3. Vòng phải ĐANG có kết quả để hủy
            var results = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(fr => fr.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);
            if (results.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi chưa có kết quả được công bố để hủy.");
            }

            // 4. Vòng SAU chưa vận hành: đã có bài nộp/kết quả ở vòng sau thì kết quả vòng này
            //    đang là nền tảng cho vòng sau — không thể rút lại.
            var laterRoundIds = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .Where(r => r.EventId == round.EventId && r.RoundNumber > round.RoundNumber)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);
            if (laterRoundIds.Count > 0)
            {
                var laterHasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                    sr => laterRoundIds.Contains(sr.RoundId), cancellationToken);
                var laterHasResults = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId != null && laterRoundIds.Contains(fr.RoundId), cancellationToken);
                if (laterHasSubmissions || laterHasResults)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Vòng sau đã có bài nộp/kết quả dựa trên kết quả vòng này nên không thể hủy công bố.");
                }
            }

            // 5. Xóa trọn bộ kết quả của vòng — ghi audit TRƯỚC SaveChanges để còn dấu vết.
            await _auditLogService.AppendAsync(
                AuditActions.UnpublishRoundResults,
                AuditEntityTypes.Round,
                round.Id,
                round.EventId,
                $"Hủy công bố vòng {round.RoundName}: xóa {results.Count} dòng FinalResult",
                new { count = results.Count, publishedCount = results.Count(r => r.IsPublished) },
                cancellationToken);
            await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(results);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


