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
        private readonly INotificationService _notificationService;

        public PublishRoundResultsCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
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

            // 5. Bắn thông báo In-App cho toàn bộ thành viên các đội tham gia vòng thi
            var teamIds = results.Select(r => r.TeamId).Distinct().ToList();
            if (teamIds.Count > 0)
            {
                var participantUserIds = await _unitOfWork.GetRepository<EventRole>().Entities
                    .Where(er => er.TeamId != null && teamIds.Contains(er.TeamId))
                    .Select(er => er.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (participantUserIds.Count > 0)
                {
                    await _notificationService.NotifyManyAsync(
                        participantUserIds,
                        "Kết quả vòng thi đã được công bố",
                        $"Kết quả thi đấu chính thức của vòng '{round.RoundName}' đã được công bố. Bạn có thể kiểm tra thứ hạng trên Bảng xếp hạng.",
                        "result",
                        $"/leaderboard?eventId={round.EventId}",
                        cancellationToken);
                }
            }

            return true;
        }
    }
}


