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

        public RespondAppealCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
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
                if (string.IsNullOrEmpty(request.AssignedJudgeId))
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Duyệt phúc khảo bắt buộc gán AssignedJudgeId (EventRole giám khảo).");
                }

                var judgeRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(request.AssignedJudgeId);
                if (judgeRole == null || judgeRole.RoleName != EventRoleType.Judge)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "AssignedJudgeId phải là vai trò Giám khảo (Judge) còn tồn tại.");
                }

                var eventId = appeal.SubmitResult.Round!.EventId;
                if (judgeRole.EventId != eventId)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Giám khảo được gán không thuộc sự kiện của đơn phúc khảo.");
                }

                var submitTrackId = appeal.SubmitResult.TrackId;
                if (!string.IsNullOrEmpty(judgeRole.TrackId) && judgeRole.TrackId != submitTrackId)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Giám khảo được gán không thuộc hạng mục của bài nộp đang phúc khảo.");
                }

                appeal.AssignedJudgeId = request.AssignedJudgeId;
            }
            appeal.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Appeal>().UpdateAsync(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


