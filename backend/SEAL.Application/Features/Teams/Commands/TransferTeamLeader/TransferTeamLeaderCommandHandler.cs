// [FLOW3-DOITHI][TransferTeamLeader] Truong nhom hien tai chuyen vai tro Truong nhom cho mot thanh vien khac trong doi.

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;

namespace SEAL_Application.Features.Teams.Commands.TransferTeamLeader
{
    public class TransferTeamLeaderCommandHandler : IRequestHandler<TransferTeamLeaderCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        // Yêu cầu chuyển quyền chờ chấp nhận trong 24 giờ.
        private static readonly TimeSpan TransferLifetime = TimeSpan.FromHours(24);

        public TransferTeamLeaderCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(TransferTeamLeaderCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Team tồn tại
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Nhóm có ID '{request.TeamId}' không tồn tại.");
            }

            // 2. Leader hiện tại của đội
            var currentLeaderRole = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstOrDefaultAsync(er => er.TeamId == request.TeamId
                                        && er.RoleName == EventRoleType.TeamLeader,
                                     cancellationToken);
            if (currentLeaderRole == null)
            {
                return BaseException.BadRequestNotFoundResponse("Nhóm chưa có Trưởng nhóm.");
            }

            // 3. Quyền: chỉ Trưởng nhóm hiện tại của đội, hoặc EventCoordinator, hoặc Admin
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCurrentLeader = currentLeaderRole.UserId == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            if (!isAdmin && !isCurrentLeader && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Trưởng nhóm hiện tại hoặc EventCoordinator mới được chuyển quyền Trưởng nhóm.");
            }

            // 4. Không chuyển cho chính Leader hiện tại
            if (request.NewLeaderUserId == currentLeaderRole.UserId)
            {
                return BaseException.BadRequestInvaildInputResponse("Người nhận đã là Trưởng nhóm hiện tại.");
            }

            // 5. Người nhận phải là Thành viên (TeamMember) của đội này
            var targetRole = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstOrDefaultAsync(er => er.UserId == request.NewLeaderUserId
                                        && er.TeamId == request.TeamId,
                                     cancellationToken);
            if (targetRole == null)
            {
                return BaseException.BadRequestNotFoundResponse("Người nhận không phải thành viên của nhóm này.");
            }
            if (targetRole.RoleName != EventRoleType.TeamMember)
            {
                return BaseException.BadRequestInvaildInputResponse("Chỉ có thể chuyển quyền cho một Thành viên trong đội.");
            }

            // 6. Không tạo hoán vai NGAY. Tạo YÊU CẦU chuyển quyền chờ người nhận chấp nhận
            //    (tái dùng bảng TeamInvitations - trạng thái TransferPending). Người nhận thấy trên chuông
            //    thông báo, bấm Chấp nhận thì mới hoán vai (xem RespondTeamInvitation).
            var invitationRepo = _unitOfWork.GetRepository<TeamInvitation>();

            // 6a. Hủy các yêu cầu chuyển quyền đang chờ khác của đội (mỗi đội chỉ 1 yêu cầu chờ) — tránh
            //     hoán vai 2 lần nếu nhiều người cùng accept.
            var existingPending = await invitationRepo.Entities
                .Where(i => i.TeamId == request.TeamId && i.Status == TeamInvitationStatus.TransferPending)
                .ToListAsync(cancellationToken);
            foreach (var old in existingPending)
            {
                old.Status = TeamInvitationStatus.Expired;
                old.RespondedAt = DateTime.UtcNow;
                await invitationRepo.UpdateAsync(old);
            }

            // 6b. Tạo yêu cầu chuyển quyền mới
            await invitationRepo.AddAsync(new TeamInvitation
            {
                TeamId = request.TeamId,
                InvitedUserId = request.NewLeaderUserId,          // người sẽ trở thành Trưởng nhóm
                InvitedByUserId = currentLeaderRole.UserId ?? string.Empty, // Trưởng nhóm hiện tại
                Status = TeamInvitationStatus.TransferPending,
                ExpiresAt = DateTime.UtcNow.Add(TransferLifetime),
                Notes = "Yêu cầu chuyển quyền Trưởng nhóm"
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


