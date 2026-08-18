// [FLOW3-DOITHI][GetMyTeamInvitation] Sinh vien xem loi moi vao doi ma minh nhan duoc.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation
{
    public class GetMyTeamInvitationQueryHandler : IRequestHandler<GetMyTeamInvitationQuery, Result<MyTeamInvitationResponseModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMyTeamInvitationQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<MyTeamInvitationResponseModel?>> Handle(GetMyTeamInvitationQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // Lấy lời mời đang chờ phản hồi cho user hiện tại vào đội này — gồm CẢ lời mời vào đội
            // (PendingAccept) LẪN yêu cầu chuyển quyền Trưởng nhóm (TransferPending), vì
            // RespondTeamInvitation xử lý được cả hai loại nhưng trước đây endpoint này chỉ lọc
            // PendingAccept -> người nhận yêu cầu chuyển quyền không có cách nào thấy để phản hồi.
            var invitation = await _unitOfWork.GetRepository<TeamInvitation>().Entities
                .Include(i => i.Team)
                .Where(i => i.TeamId == request.TeamId
                         && i.InvitedUserId == currentUserId
                         && (i.Status == TeamInvitationStatus.PendingAccept
                          || i.Status == TeamInvitationStatus.TransferPending))
                .OrderByDescending(i => i.ExpiresAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (invitation == null)
            {
                return null;
            }

            // Đang chờ (dù là mời vào đội hay chuyển quyền) nhưng đã quá hạn -> hiển thị Expired cho
            // đúng thực tế (cùng cách tính với GetTeamInvitationsQueryHandler).
            var isPending = invitation.Status == TeamInvitationStatus.PendingAccept
                          || invitation.Status == TeamInvitationStatus.TransferPending;
            var effectiveStatus = (isPending && invitation.ExpiresAt < DateTime.UtcNow)
                ? TeamInvitationStatus.Expired
                : invitation.Status;

            return new MyTeamInvitationResponseModel
            {
                InvitationId = invitation.Id,
                TeamId = invitation.TeamId,
                TeamName = invitation.Team?.Name ?? string.Empty,
                InvitedByUserId = invitation.InvitedByUserId,
                Status = effectiveStatus.ToString(),
                ExpiresAt = invitation.ExpiresAt,
                Notes = invitation.Notes
            };
        }
    }
}


