using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Appeals.Commands.RespondAppeal
{
    public class RespondAppealCommandHandler : IRequestHandler<RespondAppealCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly INotificationService _notificationService;

        public RespondAppealCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _notificationService = notificationService;
        }

        public async Task<Result<bool>> Handle(RespondAppealCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Vui lòng đăng nhập.");
            }

            if (request.Status == AppealStatus.Pending)
            {
                return BaseException.BadRequestInvaildInputResponse("Trạng thái phản hồi phải là Approved hoặc Rejected.");
            }

            var appeal = await _unitOfWork.GetRepository<Appeal>().Entities
                .Include(a => a.SubmitResult)
                    .ThenInclude(sr => sr.Round)
                .FirstOrDefaultAsync(a => a.Id == request.AppealId, cancellationToken);

            if (appeal == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy đơn phúc khảo.");
            }

            // Kiểm tra quyền: Admin hoặc EC của sự kiện
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            bool isAdmin = user != null && user.IsAdmin;
            
            bool isCoordinator = false;
            if (!isAdmin)
            {
                isCoordinator = await _eventRoleChecker.HasRoleAsync(
                    userId, appeal.SubmitResult.Round!.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            }

            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên (EC) mới có quyền duyệt đơn phúc khảo.");
            }

            // Kiểm tra trạng thái hiện tại
            if (appeal.Status != AppealStatus.Pending)
            {
                return BaseException.BadRequestInvaildInputResponse("Chỉ có thể phản hồi các đơn phúc khảo đang chờ xử lý (Pending).");
            }

            // Cập nhật
            appeal.Status = request.Status;
            appeal.Response = request.Response;
            if (request.Status == AppealStatus.Approved)
            {
                appeal.AssignedJudgeId = request.AssignedJudgeId;
            }
            appeal.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Appeal>().UpdateAsync(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Bắn thông báo In-App cho người gửi đơn
            if (!string.IsNullOrEmpty(appeal.CreatedBy))
            {
                var statusText = appeal.Status == AppealStatus.Approved ? "CHẤP THUẬN" : "TỪ CHỐI";
                var notifType = appeal.Status == AppealStatus.Approved ? "success" : "warning";
                await _notificationService.NotifyAsync(
                    appeal.CreatedBy,
                    "Phản hồi đơn phúc khảo",
                    $"Đơn phúc khảo bài thi của bạn đã được {statusText}. Phản hồi từ BTC: {appeal.Response}",
                    notifType,
                    "/appeals",
                    cancellationToken);
            }

            // Nếu đơn được chấp thuận và có chỉ định Giám khảo chấm lại, bắn thông báo cho Giám khảo
            if (appeal.Status == AppealStatus.Approved && !string.IsNullOrEmpty(appeal.AssignedJudgeId))
            {
                var judgeRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(appeal.AssignedJudgeId);
                if (judgeRole != null && !string.IsNullOrEmpty(judgeRole.UserId))
                {
                    await _notificationService.NotifyAsync(
                        judgeRole.UserId,
                        "Phân công chấm phúc khảo",
                        $"Bạn được phân công chấm lại một bài thi đã được BTC duyệt phúc khảo.",
                        "info",
                        $"/judge/scoring/{appeal.SubmitResultId}",
                        cancellationToken);
                }
            }

            return true;
        }
    }
}


