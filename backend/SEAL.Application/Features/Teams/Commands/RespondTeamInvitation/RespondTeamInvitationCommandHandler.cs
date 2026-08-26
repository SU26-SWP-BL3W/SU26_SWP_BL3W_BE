// [FLOW3-DOITHI][RespondTeamInvitation] Sinh vien duoc moi Dong y/Tu choi loi moi vao doi.

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Commands.RespondTeamInvitation.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.RespondTeamInvitation
{
    public class RespondTeamInvitationCommandHandler : IRequestHandler<RespondTeamInvitationCommand, Result<RespondTeamInvitationResponseModel>>
    {
        private const int MAX_TEAM_SIZE = 5;
        private const string TRANSFER_NOTES = "Yêu cầu chuyển quyền Trưởng nhóm";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly INotificationService _notifications;
        private readonly IEmailService _emailService;
        private readonly ILogger<RespondTeamInvitationCommandHandler> _logger;
        private readonly string _frontendUrl;

        public RespondTeamInvitationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            INotificationService notifications,
            IEmailService emailService,
            ILogger<RespondTeamInvitationCommandHandler> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _notifications = notifications;
            _emailService = emailService;
            _logger = logger;
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        private async Task SendResponseEmailAsync(string toEmail, string toName, string heading, string introHtml,
            EmailTemplate.Callout kind, string subject, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(toEmail)) return;
            var body = EmailTemplate.Render(
                heading: heading,
                greetingName: toName,
                introHtml: introHtml,
                calloutLabel: kind == EmailTemplate.Callout.Success ? "Đã xác nhận" : "Đã từ chối",
                calloutHtml: kind == EmailTemplate.Callout.Success
                    ? "Vào mục <b>Đội thi của tôi</b> để xem chi tiết."
                    : "Bạn vẫn giữ vai trò Trưởng nhóm — không cần làm gì thêm.",
                calloutKind: kind,
                ctaText: "Xem đội thi",
                ctaUrl: $"{_frontendUrl}/my-team",
                ctaFallbackUrl: _frontendUrl,
                showLoginHint: false);
            try { await _emailService.SendEmailAsync(toEmail, subject, body); }
            catch (Exception ex) { _logger.LogWarning(ex, "Gửi email phản hồi chuyển quyền Trưởng nhóm thất bại cho {Email}", toEmail); }
        }

        public async Task<Result<RespondTeamInvitationResponseModel>> Handle(RespondTeamInvitationCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Tìm lời mời
            var invitation = await _unitOfWork.GetRepository<TeamInvitation>().GetByIdAsync(request.InvitationId);
            if (invitation == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Lời mời '{request.InvitationId}' không tồn tại.");
            }

            // 2. Chỉ chính người được mời mới được phản hồi
            if (invitation.InvitedUserId != currentUserId)
            {
                return new BaseException.ForbiddenException("Bạn không phải người được mời trong lời mời này.");
            }

            // 3. Lời mời phải đang chờ phản hồi (mời vào đội HOẶC yêu cầu chuyển quyền Trưởng nhóm)
            if (invitation.Status != TeamInvitationStatus.PendingAccept
             && invitation.Status != TeamInvitationStatus.TransferPending)
            {
                return BaseException.BadRequestInvaildInputResponse($"Lời mời đã được xử lý trước đó (trạng thái: {invitation.Status}).");
            }

            // 4. Lazy expire: nếu quá hạn thì đánh dấu Expired và báo lỗi
            var now = DateTime.UtcNow;
            if (invitation.ExpiresAt <= now)
            {
                invitation.Status = TeamInvitationStatus.Expired;
                invitation.RespondedAt = now;
                await _unitOfWork.GetRepository<TeamInvitation>().UpdateAsync(invitation);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return BaseException.BadRequestInvaildInputResponse("Lời mời đã hết hạn.");
            }

            // 5. DECLINE — từ chối lời mời (mời vào đội HOẶC yêu cầu chuyển quyền Trưởng nhóm)
            if (!request.IsAccepted)
            {
                bool wasTransfer = invitation.Status == TeamInvitationStatus.TransferPending
                    || invitation.Notes == TRANSFER_NOTES;

                invitation.Status = TeamInvitationStatus.Declined;
                invitation.RespondedAt = now;
                await _unitOfWork.GetRepository<TeamInvitation>().UpdateAsync(invitation);

                var leaderId = await _unitOfWork.GetRepository<EventRole>().Entities
                    .Where(er => er.TeamId == invitation.TeamId && er.RoleName == EventRoleType.TeamLeader)
                    .Select(er => er.UserId)
                    .FirstOrDefaultAsync(cancellationToken);
                var declineTeam = await _unitOfWork.GetRepository<Team>().GetByIdAsync(invitation.TeamId);
                var teamName = declineTeam?.Name ?? "đội";
                var respondingUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
                var responderName = respondingUser?.FullName ?? "Một thành viên";

                if (!string.IsNullOrEmpty(leaderId))
                {
                    await _notifications.NotifyAsync(
                        leaderId,
                        wasTransfer ? "Yêu cầu chuyển quyền bị từ chối" : "Lời mời bị từ chối",
                        wasTransfer
                            ? $"{responderName} đã từ chối lời mời chuyển quyền Trưởng nhóm đội {teamName}. Bạn vẫn là Trưởng nhóm."
                            : $"Một thành viên đã từ chối lời mời vào đội {teamName}.",
                        "warning",
                        "/my-team",
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Email báo người gửi (Trưởng nhóm) CHỈ áp dụng cho yêu cầu chuyển quyền — lời mời
                // vào đội thường vốn đã không có email báo Leader lúc bị từ chối (giữ nguyên hành vi cũ).
                if (wasTransfer && !string.IsNullOrEmpty(leaderId))
                {
                    var leaderUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(leaderId);
                    if (leaderUser != null)
                    {
                        await SendResponseEmailAsync(
                            leaderUser.Email, leaderUser.FullName,
                            "Yêu cầu chuyển quyền Trưởng nhóm bị từ chối",
                            $"<b>{responderName}</b> đã <b>từ chối</b> lời mời chuyển quyền Trưởng nhóm đội <b>{teamName}</b>. Bạn vẫn tiếp tục là Trưởng nhóm.",
                            EmailTemplate.Callout.Danger,
                            $"[SEAL] Yêu cầu chuyển quyền Trưởng nhóm đội {teamName} bị từ chối",
                            cancellationToken);
                    }
                }

                return new RespondTeamInvitationResponseModel
                {
                    InvitationId = invitation.Id,
                    TeamId = invitation.TeamId,
                    Status = invitation.Status.ToString()
                };
            }

            // 5b. ACCEPT + TransferPending — HOÁN VAI Trưởng nhóm (không thêm thành viên mới).
            if (invitation.Status == TeamInvitationStatus.TransferPending)
            {
                var transferTeam = await _unitOfWork.GetRepository<Team>().GetByIdAsync(invitation.TeamId);
                if (transferTeam == null)
                {
                    return BaseException.BadRequestNotFoundResponse("Đội của yêu cầu này không còn tồn tại.");
                }

                // Người chấp nhận phải VẪN là Thành viên của đội
                var newLeaderRole = await _unitOfWork.GetRepository<EventRole>().Entities
                    .FirstOrDefaultAsync(er => er.UserId == currentUserId && er.TeamId == invitation.TeamId,
                                         cancellationToken);
                if (newLeaderRole == null)
                {
                    return BaseException.BadRequestInvaildInputResponse("Bạn không còn là thành viên của đội này.");
                }

                var oldLeaderRole = await _unitOfWork.GetRepository<EventRole>().Entities
                    .FirstOrDefaultAsync(er => er.TeamId == invitation.TeamId
                                            && er.RoleName == EventRoleType.TeamLeader,
                                         cancellationToken);

                // Hoán vai (nếu vì lý do nào đó người chấp nhận đã là leader thì chỉ chốt invitation)
                if (oldLeaderRole != null && oldLeaderRole.UserId != currentUserId)
                {
                    oldLeaderRole.RoleName = EventRoleType.TeamMember;
                    oldLeaderRole.LastUpdatedTime = CoreHelper.SystemTimeNow;
                    await _unitOfWork.GetRepository<EventRole>().UpdateAsync(oldLeaderRole);

                    newLeaderRole.RoleName = EventRoleType.TeamLeader;
                    newLeaderRole.LastUpdatedTime = CoreHelper.SystemTimeNow;
                    await _unitOfWork.GetRepository<EventRole>().UpdateAsync(newLeaderRole);
                }

                invitation.Status = TeamInvitationStatus.Accepted;
                invitation.RespondedAt = now;
                await _unitOfWork.GetRepository<TeamInvitation>().UpdateAsync(invitation);

                // Báo cho Trưởng nhóm CŨ là chuyển quyền đã hoàn tất — trước giờ hoán vai xong không ai
                // được báo cả, chỉ tự nhận ra khi thấy quyền của mình đổi.
                if (oldLeaderRole?.UserId != null)
                {
                    await _notifications.NotifyAsync(
                        oldLeaderRole.UserId,
                        "Đã chuyển quyền Trưởng nhóm",
                        $"Bạn đã chuyển quyền Trưởng nhóm {transferTeam.Name} thành công.",
                        "info",
                        "/my-team",
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Email báo Trưởng nhóm CŨ — trước đây chỉ có thông báo chuông (NotifyAsync ở trên),
                // chưa có email nhắc khi bên kia đã Đồng ý nhận quyền.
                if (oldLeaderRole?.UserId != null)
                {
                    var oldLeaderUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(oldLeaderRole.UserId);
                    var newLeaderUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
                    if (oldLeaderUser != null)
                    {
                        var newLeaderName = newLeaderUser?.FullName ?? "Thành viên được đề xuất";
                        await SendResponseEmailAsync(
                            oldLeaderUser.Email, oldLeaderUser.FullName,
                            "Chuyển quyền Trưởng nhóm thành công",
                            $"<b>{newLeaderName}</b> đã <b>đồng ý</b> nhận quyền Trưởng nhóm đội <b>{transferTeam.Name}</b>. Việc chuyển quyền đã hoàn tất.",
                            EmailTemplate.Callout.Success,
                            $"[SEAL] Đã chuyển quyền Trưởng nhóm đội {transferTeam.Name} thành công",
                            cancellationToken);
                    }
                }

                // Xoá cache phân quyền 2 người vừa đổi vai (leader mới thao tác được ngay)
                _eventRoleChecker.InvalidateCache(currentUserId, transferTeam.EventId);
                if (oldLeaderRole?.UserId != null)
                {
                    _eventRoleChecker.InvalidateCache(oldLeaderRole.UserId, transferTeam.EventId);
                }

                return new RespondTeamInvitationResponseModel
                {
                    InvitationId = invitation.Id,
                    TeamId = invitation.TeamId,
                    Status = invitation.Status.ToString(),
                    EventRoleId = newLeaderRole.Id
                };
            }

            // 6. ACCEPT — re-validate trước khi thêm vào đội
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(invitation.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse("Đội của lời mời này không còn tồn tại.");
            }
            if (team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã đăng ký/khóa, không thể tham gia.");
            }

            // 6a'. Còn trong HẠN ĐĂNG KÝ sự kiện: lời mời gửi trong hạn nhưng bấm chấp nhận SAU khi
            //      hết hạn đăng ký thì không được vào đội nữa (Invite chỉ check lúc gửi).
            var invitedEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(team.EventId);
            if (invitedEvent != null && invitedEvent.RegistrationEndDate.HasValue && now > invitedEvent.RegistrationEndDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian đăng ký thi nên không thể tham gia đội.");
            }

            // 6a. Account chưa bị khóa (vẫn approved)
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (currentUser == null || !currentUser.IsApproved)
            {
                return BaseException.BadRequestInvaildInputResponse("Tài khoản của bạn chưa được phê duyệt hoặc đã bị khóa.");
            }

            // 6a2. Phải hoàn tất HỒ SƠ THÍ SINH (là sinh viên + đã có trường) mới được CHẤP NHẬN vào đội.
            //      Người được mời qua email chưa đăng ký: kích hoạt tài khoản -> cập nhật hồ sơ -> được duyệt -> mới accept được.
            //      (Vẫn có thể TỪ CHỐI bất cứ lúc nào ở nhánh Decline phía trên — không cần hồ sơ.)
            if (!currentUser.IsStudent || string.IsNullOrEmpty(currentUser.SchoolId))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Bạn cần cập nhật hồ sơ sinh viên và được duyệt trước khi chấp nhận tham gia đội.");
            }

            // 6b. Vẫn chưa tham gia đội khác trong sự kiện này
            var alreadyInTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.EventId == team.EventId
                   && er.TeamId != null
                   && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                cancellationToken);
            if (alreadyInTeam)
            {
                return BaseException.BadRequestInvaildInputResponse("Bạn đã tham gia một đội khác trong sự kiện này.");
            }

            // 6c. Đội chưa đầy
            var currentMemberCount = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == invitation.TeamId
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .CountAsync(cancellationToken);
            if (currentMemberCount >= MAX_TEAM_SIZE)
            {
                return BaseException.BadRequestInvaildInputResponse($"Đội đã đủ tối đa {MAX_TEAM_SIZE} thành viên.");
            }

            // 6d. Tạo EventRole = TeamMember + đánh dấu lời mời Accepted
            var memberRole = new EventRole
            {
                UserId = currentUserId,
                EventId = team.EventId,
                TeamId = team.Id,
                RoleName = EventRoleType.TeamMember,
                AssignedAt = now,
                ExpiredAt = invitedEvent?.EndDate,
                Notes = "Tham gia đội qua lời mời"
            };
            await _unitOfWork.GetRepository<EventRole>().AddAsync(memberRole);

            invitation.Status = TeamInvitationStatus.Accepted;
            invitation.RespondedAt = now;
            await _unitOfWork.GetRepository<TeamInvitation>().UpdateAsync(invitation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6e. CHỐNG RACE đầy đội: 2 lời mời được chấp nhận gần như đồng thời có thể cùng qua
            //     bước đếm 6c (đếm-rồi-mới-ghi có khoảng hở) -> đội vượt trần 5 người. Sau khi lưu,
            //     đếm lại: nếu vượt, người vào SAU (CreatedTime lớn hơn, hòa thì so Id) tự rút —
            //     quy tắc xác định nên mọi request race đều hội tụ về đúng 5 thành viên.
            var memberSnapshots = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                .AsNoTracking()
                .Where(er => er.TeamId == invitation.TeamId
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .Select(er => new { er.Id, er.CreatedTime })
                .ToListAsync(cancellationToken);
            if (memberSnapshots.Count > MAX_TEAM_SIZE)
            {
                var keepIds = memberSnapshots
                    .OrderBy(m => m.CreatedTime)
                    .ThenBy(m => m.Id, StringComparer.Ordinal)
                    .Take(MAX_TEAM_SIZE)
                    .Select(m => m.Id)
                    .ToHashSet();
                if (!keepIds.Contains(memberRole.Id))
                {
                    await _unitOfWork.GetRepository<EventRole>().DeleteAsync(memberRole);
                    invitation.Status = TeamInvitationStatus.Expired; // slot cuối đã bị lấy trước
                    await _unitOfWork.GetRepository<TeamInvitation>().UpdateAsync(invitation);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return BaseException.BadRequestInvaildInputResponse($"Đội đã đủ tối đa {MAX_TEAM_SIZE} thành viên.");
                }
            }

            // Thông báo trưởng nhóm: thành viên đã tham gia
            var acceptLeaderId = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == invitation.TeamId && er.RoleName == EventRoleType.TeamLeader)
                .Select(er => er.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            var acceptTeam = await _unitOfWork.GetRepository<Team>().GetByIdAsync(invitation.TeamId);
            if (!string.IsNullOrEmpty(acceptLeaderId))
            {
                await _notifications.NotifyAsync(
                    acceptLeaderId,
                    "Thành viên đã tham gia",
                    $"Một thành viên đã đồng ý vào đội {acceptTeam?.Name ?? ""}.",
                    "success",
                    "/my-team",
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new RespondTeamInvitationResponseModel
            {
                InvitationId = invitation.Id,
                TeamId = invitation.TeamId,
                Status = invitation.Status.ToString(),
                EventRoleId = memberRole.Id
            };
        }
    }
}


