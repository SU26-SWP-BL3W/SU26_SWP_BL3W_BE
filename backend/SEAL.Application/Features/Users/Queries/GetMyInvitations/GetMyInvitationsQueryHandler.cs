using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Users.Queries.GetMyInvitations.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Queries.GetMyInvitations
{
    /// <summary>
    /// Chuông thông báo: gộp lời mời của người dùng hiện tại.
    /// - PendingAccept (còn hạn): hiển thị kèm nút Đồng ý/Từ chối.
    /// - Đã phản hồi gần đây (Accepted/Rejected trong 7 ngày): hiển thị làm thông báo lịch sử
    ///   ("đã nhận vai trò X" / "đã từ chối"), không còn nút.
    /// totalPending chỉ đếm các lời mời còn chờ.
    /// </summary>
    public class GetMyInvitationsQueryHandler : IRequestHandler<GetMyInvitationsQuery, Result<MyInvitationsResponseModel>>
    {
        private const string TYPE_TEAM = "TEAM";
        private const string TYPE_EVENT_ROLE = "EVENT_ROLE";
        private const string TEAM_MEMBER_ROLE_LABEL = "Thành viên";
        private const string TEAM_LEADER_ROLE_LABEL = "Trưởng nhóm";
        private const string STATUS_PENDING = "PendingAccept";
        private const int HISTORY_DAYS = 7;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMyInvitationsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        /// <summary>Chuẩn hóa trạng thái lời mời vai trò về chuỗi FE dùng: Accepted | Declined | PendingAccept.</summary>
        private static string MapStatus(EventRoleInvitationStatus s) => s switch
        {
            EventRoleInvitationStatus.Accepted => "Accepted",
            EventRoleInvitationStatus.Rejected => "Declined",
            _ => STATUS_PENDING
        };

        /// <summary>Chuẩn hóa trạng thái lời mời đội (join lẫn chuyển quyền) về chuỗi FE dùng.</summary>
        private static string MapTeamStatus(TeamInvitationStatus s) => s switch
        {
            TeamInvitationStatus.Accepted => "Accepted",
            TeamInvitationStatus.Declined => "Declined",
            TeamInvitationStatus.Expired => "Expired",
            TeamInvitationStatus.Cancelled => "Cancelled",
            _ => STATUS_PENDING
        };

        public async Task<Result<MyInvitationsResponseModel>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var now = DateTime.UtcNow;
            var historyCutoff = now.AddDays(-HISTORY_DAYS);

            // 1. Lời mời vào đội (TeamInvitation) đang chờ và còn hiệu lực
            var teamInvites = await _unitOfWork.GetRepository<TeamInvitation>().GetQueryable()
                .AsNoTracking()
                .Include(i => i.Team)
                .Where(i => i.InvitedUserId == currentUserId
                         && (i.Status == TeamInvitationStatus.PendingAccept
                          || i.Status == TeamInvitationStatus.TransferPending)
                         && i.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            // 1b. Lời mời vào đội (join lẫn chuyển quyền Trưởng nhóm) ĐÃ phản hồi/hết hạn/bị hủy gần đây
            //     (trong HISTORY_DAYS ngày) — làm thông báo lịch sử, cùng cách EventRoleInvitation đã làm
            //     ở dưới (mục 3). Trước đây chỉ có EventRoleInvitation có lịch sử, TeamInvitation thì không.
            var respondedTeamInvites = await _unitOfWork.GetRepository<TeamInvitation>().GetQueryable()
                .AsNoTracking()
                .Include(i => i.Team)
                .Where(i => i.InvitedUserId == currentUserId
                         && (i.Status == TeamInvitationStatus.Accepted
                          || i.Status == TeamInvitationStatus.Declined
                          || i.Status == TeamInvitationStatus.Expired
                          || i.Status == TeamInvitationStatus.Cancelled)
                         && i.RespondedAt != null
                         && i.RespondedAt >= historyCutoff)
                .OrderByDescending(i => i.RespondedAt)
                .ToListAsync(cancellationToken);

            // 2. Lời mời vai trò sự kiện đang chờ và còn hiệu lực
            var pendingRoleInvites = await _unitOfWork.GetRepository<EventRoleInvitation>().GetQueryable()
                .AsNoTracking()
                .Include(i => i.Event)
                .Include(i => i.InvitedByUser)
                .Include(i => i.Track)
                .Where(i => i.InvitedUserId == currentUserId
                         && i.Status == EventRoleInvitationStatus.Pending
                         && i.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            // 3. Lời mời vai trò ĐÃ phản hồi gần đây (Accepted/Rejected trong HISTORY_DAYS ngày) — làm thông báo lịch sử
            var respondedRoleInvites = await _unitOfWork.GetRepository<EventRoleInvitation>().GetQueryable()
                .AsNoTracking()
                .Include(i => i.Event)
                .Include(i => i.InvitedByUser)
                .Include(i => i.Track)
                .Where(i => i.InvitedUserId == currentUserId
                         && (i.Status == EventRoleInvitationStatus.Accepted || i.Status == EventRoleInvitationStatus.Rejected)
                         && i.RespondedAt != null
                         && i.RespondedAt >= historyCutoff)
                .OrderByDescending(i => i.RespondedAt)
                .ToListAsync(cancellationToken);

            // 4. Tra cứu tên người gửi mời cho TeamInvitation (bảng này không có navigation tới người mời)
            var teamInviterIds = teamInvites.Select(t => t.InvitedByUserId)
                .Concat(respondedTeamInvites.Select(t => t.InvitedByUserId))
                .Distinct().ToList();
            var inviterNames = teamInviterIds.Count == 0
                ? new Dictionary<string, string>()
                : await _unitOfWork.GetRepository<User>().GetQueryable()
                    .AsNoTracking()
                    .Where(u => teamInviterIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName })
                    .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

            var pending = new List<InvitationItemModel>();
            var history = new List<InvitationItemModel>();

            foreach (var t in teamInvites)
            {
                // TransferPending = yêu cầu chuyển quyền Trưởng nhóm. Cả mời-vào-đội lẫn chuyển-quyền đều
                // dùng CHUNG endpoint phản hồi POST /Teams/invitations/{id}/respond (vì đều là TeamInvitation).
                // => Type = "TEAM" để chuông FE route đúng về endpoint đội; phân biệt "chuyển quyền" qua Role.
                bool isTransfer = t.Status == TeamInvitationStatus.TransferPending;
                pending.Add(new InvitationItemModel
                {
                    InvitationId = t.Id,
                    Type = TYPE_TEAM,
                    TargetName = t.Team?.Name ?? string.Empty,
                    InviterName = inviterNames.TryGetValue(t.InvitedByUserId, out var name) ? name : string.Empty,
                    Role = isTransfer ? TEAM_LEADER_ROLE_LABEL : TEAM_MEMBER_ROLE_LABEL,
                    Status = STATUS_PENDING,
                    RespondedAt = null,
                    ExpiresAt = t.ExpiresAt
                });
            }

            foreach (var t in respondedTeamInvites)
            {
                bool isTransfer = t.Notes == "Yêu cầu chuyển quyền Trưởng nhóm";
                history.Add(new InvitationItemModel
                {
                    InvitationId = t.Id,
                    Type = TYPE_TEAM,
                    TargetName = t.Team?.Name ?? string.Empty,
                    InviterName = inviterNames.TryGetValue(t.InvitedByUserId, out var respName) ? respName : string.Empty,
                    Role = isTransfer ? TEAM_LEADER_ROLE_LABEL : TEAM_MEMBER_ROLE_LABEL,
                    Status = MapTeamStatus(t.Status),      // Accepted | Declined | Expired | Cancelled
                    RespondedAt = t.RespondedAt,
                    ExpiresAt = t.ExpiresAt
                });
            }

            foreach (var e in pendingRoleInvites)
            {
                pending.Add(new InvitationItemModel
                {
                    InvitationId = e.Id,
                    Type = TYPE_EVENT_ROLE,
                    TargetName = e.Event?.EventName ?? string.Empty,
                    InviterName = e.InvitedByUser?.FullName ?? string.Empty,
                    Role = e.RoleName.ToString(),
                    TrackName = e.Track?.TrackName,
                    Status = STATUS_PENDING,
                    RespondedAt = null,
                    ExpiresAt = e.ExpiresAt
                });
            }

            foreach (var e in respondedRoleInvites)
            {
                history.Add(new InvitationItemModel
                {
                    InvitationId = e.Id,
                    Type = TYPE_EVENT_ROLE,
                    TargetName = e.Event?.EventName ?? string.Empty,
                    InviterName = e.InvitedByUser?.FullName ?? string.Empty,
                    Role = e.RoleName.ToString(),
                    TrackName = e.Track?.TrackName,
                    Status = MapStatus(e.Status),      // Accepted | Declined
                    RespondedAt = e.RespondedAt,
                    ExpiresAt = e.ExpiresAt
                });
            }

            // Pending (sắp hết hạn trước) rồi tới lịch sử (mới phản hồi trước)
            var ordered = pending.OrderBy(i => i.ExpiresAt)
                .Concat(history.OrderByDescending(i => i.RespondedAt))
                .ToList();

            return new MyInvitationsResponseModel
            {
                TotalPending = pending.Count,   // badge chỉ đếm lời mời còn chờ
                Invitations = ordered
            };
        }
    }
}


