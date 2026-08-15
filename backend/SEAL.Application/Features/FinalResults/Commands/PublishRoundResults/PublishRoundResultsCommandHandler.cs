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

namespace SEAL_Application.Features.FinalResults.Commands.PublishRoundResults
{
    /// <summary>
    /// CÔNG BỐ kết quả vòng thi: bật IsPublished = true cho TRỌN BỘ FinalResult của vòng.
    /// Luồng: CalculateRoundResults tạo bản NHÁP (IsPublished=false, chỉ EC/Admin xem)
    /// -> EC rà soát -> Publish -> mọi người xem được bảng xếp hạng.
    /// Chỉ EventCoordinator (hoặc Admin) — đối xứng với Calculate/Unpublish.
    /// </summary>
    public class PublishRoundResultsCommandHandler : IRequestHandler<PublishRoundResultsCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IAuditLogService _auditLogService;

        public PublishRoundResultsCommandHandler(
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

        public async Task<Result<bool>> Handle(PublishRoundResultsCommand request, CancellationToken cancellationToken)
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
                return new BaseException.ForbiddenException("Chỉ EventCoordinator được công bố kết quả vòng thi.");
            }

            // 3. Vòng phải ĐÃ được tính kết quả (có FinalResult) mới công bố được
            var results = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(fr => fr.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);
            if (results.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vòng thi chưa có kết quả được tính (hãy tính kết quả trước khi công bố).");
            }

            // 4. Bật công bố cho toàn bộ kết quả của vòng (idempotent: đã công bố thì giữ nguyên)
            foreach (var fr in results)
            {
                fr.IsPublished = true;
            }
            await _unitOfWork.GetRepository<FinalResult>().UpdateRangeAsync(results);
            await _auditLogService.AppendAsync(
                AuditActions.PublishRoundResults,
                AuditEntityTypes.Round,
                round.Id,
                round.EventId,
                $"Công bố kết quả vòng {round.RoundName}: {results.Count} dòng",
                new { count = results.Count },
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


