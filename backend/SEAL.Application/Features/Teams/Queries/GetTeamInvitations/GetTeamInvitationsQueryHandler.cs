using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Teams.Queries.GetTeamInvitations.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Features.Teams.Queries.GetTeamInvitations
{
    public class GetTeamInvitationsQueryHandler : IRequestHandler<GetTeamInvitationsQuery, Result<List<TeamInvitationListItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetTeamInvitationsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<TeamInvitationListItemModel>>> Handle(GetTeamInvitationsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            if (string.IsNullOrEmpty(request.TeamId))
            {
                return BaseException.BadRequestResponse("ID đội (TeamId) là bắt buộc.");
            }

            var now = DateTime.UtcNow;

            // Quyền xem đã được EventRoleAuthorize (TeamLeader của đội / EventCoordinator) chặn ở controller.
            var invitations = await _unitOfWork.GetRepository<TeamInvitation>().Entities
                .AsNoTracking()
                .Include(i => i.InvitedUser)
                .Where(i => i.TeamId == request.TeamId)
                .OrderByDescending(i => i.CreatedTime)
                .ToListAsync(cancellationToken);

            return invitations.Select(i =>
            {
                // PendingAccept nhưng đã quá hạn -> hiển thị Expired cho đúng thực tế.
                var effectiveStatus = (i.Status == TeamInvitationStatus.PendingAccept && i.ExpiresAt < now)
                    ? TeamInvitationStatus.Expired
                    : i.Status;

                return new TeamInvitationListItemModel
                {
                    InvitationId = i.Id,
                    TeamId = i.TeamId,
                    InvitedUserId = i.InvitedUserId,
                    InvitedUserFullName = i.InvitedUser?.FullName ?? string.Empty,
                    InvitedUserEmail = i.InvitedUser?.Email ?? string.Empty,
                    InvitedUserStudentCode = i.InvitedUser?.StudentCode,
                    InvitedByUserId = i.InvitedByUserId,
                    Status = effectiveStatus.ToString(),
                    StatusLabel = effectiveStatus switch
                    {
                        TeamInvitationStatus.PendingAccept => "Đang chờ xác nhận",
                        TeamInvitationStatus.Accepted => "Đã tham gia",
                        TeamInvitationStatus.Declined => "Đã từ chối",
                        TeamInvitationStatus.Expired => "Hết hạn",
                        _ => effectiveStatus.ToString()
                    },
                    ExpiresAt = i.ExpiresAt,
                    RespondedAt = i.RespondedAt,
                    Notes = i.Notes,
                    CreatedTime = i.CreatedTime
                };
            }).ToList();
        }
    }
}




