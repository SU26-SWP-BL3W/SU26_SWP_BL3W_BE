using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models;

namespace SEAL_Application.Features.EventRoles.Commands.DeclineEventRoleInvitation
{
    /// <summary>
    /// Từ chối lời mời vai trò qua LINK EMAIL — KHÔNG cần đăng nhập, chỉ cần ID lời mời.
    /// An toàn vì chỉ đánh dấu Rejected (không tạo vai trò nào). Chấp nhận vẫn phải đăng nhập.
    /// </summary>
    public class DeclineEventRoleInvitationCommand : IRequest<Result<RespondEventRoleInvitationResponseModel>>
    {
        public string InvitationId { get; set; } = string.Empty;

        public DeclineEventRoleInvitationCommand(string invitationId)
        {
            InvitationId = invitationId;
        }
    }
}

