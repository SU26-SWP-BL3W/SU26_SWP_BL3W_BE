using MediatR;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.DeclineEventRoleInvitation
{
    /// <summary>
    /// Xử lý từ chối lời mời qua link email (không cần đăng nhập).
    /// - Pending còn hạn  -> đánh dấu Rejected.
    /// - Pending quá hạn  -> đánh dấu Expired.
    /// - Đã phản hồi rồi  -> trả về trạng thái hiện tại (idempotent, không báo lỗi để trang cảm ơn vẫn hiện).
    /// KHÔNG tạo EventRole, nên an toàn khi mở công khai (ID lời mời là GUID, chỉ có trong email người được mời).
    /// </summary>
    public class DeclineEventRoleInvitationCommandHandler : IRequestHandler<DeclineEventRoleInvitationCommand, Result<RespondEventRoleInvitationResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notifications;

        public DeclineEventRoleInvitationCommandHandler(IUnitOfWork unitOfWork, INotificationService notifications)
        {
            _unitOfWork = unitOfWork;
            _notifications = notifications;
        }

        public async Task<Result<RespondEventRoleInvitationResponseModel>> Handle(DeclineEventRoleInvitationCommand request, CancellationToken cancellationToken)
        {
            var invitation = await _unitOfWork.GetRepository<EventRoleInvitation>().GetByIdAsync(request.InvitationId);
            if (invitation == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Lời mời '{request.InvitationId}' không tồn tại.");
            }

            var now = DateTime.UtcNow;

            // Đã CHẤP NHẬN trước đó thì không thể từ chối (tránh hiển thị "đã từ chối" sai lệch khi vai trò đã được tạo).
            if (invitation.Status == EventRoleInvitationStatus.Accepted)
            {
                return BaseException.BadRequestInvaildInputResponse("Bạn đã chấp nhận lời mời này trước đó nên không thể từ chối.");
            }

            // Chưa phản hồi -> xử lý; đã Từ chối/Hết hạn -> giữ nguyên (idempotent, trang cảm ơn vẫn hiện).
            if (invitation.Status == EventRoleInvitationStatus.Pending)
            {
                invitation.Status = invitation.ExpiresAt <= now
                    ? EventRoleInvitationStatus.Expired
                    : EventRoleInvitationStatus.Rejected;
                invitation.RespondedAt = now;
                await _unitOfWork.GetRepository<EventRoleInvitation>().UpdateAsync(invitation);

                if (invitation.Status == EventRoleInvitationStatus.Rejected && !string.IsNullOrEmpty(invitation.InvitedByUserId))
                {
                    await _notifications.NotifyAsync(
                        invitation.InvitedByUserId,
                        "Lời mời vai trò bị từ chối",
                        $"Một lời mời đảm nhận vai trò {invitation.RoleName} đã bị từ chối.",
                        "warning",
                        "/coordinator/staff",
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new RespondEventRoleInvitationResponseModel
            {
                InvitationId = invitation.Id,
                EventId = invitation.EventId,
                Status = invitation.Status.ToString()
            };
        }
    }
}

