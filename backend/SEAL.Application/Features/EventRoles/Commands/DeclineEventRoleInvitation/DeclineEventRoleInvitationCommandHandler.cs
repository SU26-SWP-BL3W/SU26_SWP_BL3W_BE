using MediatR;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models;
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

        public DeclineEventRoleInvitationCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

