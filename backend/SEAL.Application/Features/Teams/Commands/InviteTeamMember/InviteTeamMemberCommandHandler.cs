using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Commands.InviteTeamMember.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.InviteTeamMember
{
    public class InviteTeamMemberCommandHandler : IRequestHandler<InviteTeamMemberCommand, Result<InviteTeamMemberResponseModel>>
    {
        // Đội tối đa 5 thành viên (tính cả Leader) — theo yêu cầu "đội 3-5 thành viên"
        private const int MAX_TEAM_SIZE = 5;
        // Thời hạn lời mời (giờ) — chỉnh ở đây nếu muốn đổi
        private const int INVITATION_EXPIRY_HOURS = 24;
        // Thời hạn link KÍCH HOẠT tài khoản tạm (giờ) — để bằng lời mời cho đồng bộ toàn hệ thống
        private const int ACTIVATION_EXPIRY_HOURS = 24;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public InviteTeamMemberCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<Result<InviteTeamMemberResponseModel>> Handle(InviteTeamMemberCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Team tồn tại kèm Event
            var team = await _unitOfWork.GetRepository<Team>().Entities
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Nhóm có ID '{request.TeamId}' không tồn tại.");
            }

            // 2. Đội phải đang ở trạng thái Forming (chưa khóa đăng ký)
            if (team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã đăng ký/khóa, không thể mời thêm thành viên.");
            }

            // 2.1. Kiểm tra thời gian đăng ký thi
            var currentUtcNow = DateTime.UtcNow;
            if (team.Event.RegistrationStartDate.HasValue && currentUtcNow < team.Event.RegistrationStartDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse("Chưa đến thời gian đăng ký thi, không thể mời thành viên.");
            }
            if (team.Event.RegistrationEndDate.HasValue && currentUtcNow > team.Event.RegistrationEndDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian đăng ký thi, không thể mời thành viên.");
            }

            // 3. Quyền: caller phải là TeamLeader của đội này (hoặc Coordinator/Admin)
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isLeaderOfThisTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.TeamId == request.TeamId
                   && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            if (!isAdmin && !isLeaderOfThisTeam && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ TeamLeader của đội hoặc EventCoordinator được mời thành viên.");
            }

            // 4. Tìm kiếm người dùng được mời qua Email
            // Chuẩn hóa email (bỏ khoảng trắng + chữ thường) để không tạo 2 tài khoản 'Foo@x.com'/'foo@x.com'
            var invitedEmail = request.Model.Email.Trim().ToLowerInvariant();
            var invitedUser = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.Email.ToLower() == invitedEmail, cancellationToken);

            var frontendUrl = (_configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var eventName = team.Event?.EventName ?? "hệ thống SEAL";

            // Nếu người dùng CHƯA có tài khoản: tạo tài khoản TẠM (IsTemporary) + gửi mail kích hoạt,
            // VÀ VẪN LƯU lời mời (Pending). Nhờ đó sau khi họ kích hoạt + cập nhật hồ sơ + được duyệt,
            // đăng nhập vào là thấy lời mời và chấp nhận được (không bị mất lời mời như trước).
            if (invitedUser == null)
            {
                // Đội phải chưa đầy — tính CẢ lời mời đang chờ, để 1 leader không thể spam
                // tạo vô hạn tài khoản tạm/email qua endpoint này
                var memberCountForNew = await _unitOfWork.GetRepository<EventRole>().Entities
                    .Where(er => er.TeamId == request.TeamId
                              && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                    .CountAsync(cancellationToken);
                var pendingInviteCount = await _unitOfWork.GetRepository<TeamInvitation>().Entities
                    .Where(inv => inv.TeamId == request.TeamId
                               && inv.Status == TeamInvitationStatus.PendingAccept
                               && inv.ExpiresAt > currentUtcNow)
                    .CountAsync(cancellationToken);
                if (memberCountForNew + pendingInviteCount >= MAX_TEAM_SIZE)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        $"Đội đã đủ tối đa {MAX_TEAM_SIZE} thành viên (tính cả lời mời đang chờ phản hồi).");
                }

                var verificationToken = Guid.NewGuid().ToString();
                var tempUser = new User
                {
                    Email = invitedEmail,
                    SchoolId = null,
                    FullName = invitedEmail.Split('@')[0],
                    IsTemporary = true,
                    IsApproved = false,
                    IsEmailVerified = false,
                    EmailVerificationToken = verificationToken,
                    EmailVerificationExpiry = DateTime.UtcNow.AddHours(ACTIVATION_EXPIRY_HOURS)
                };
                await _unitOfWork.GetRepository<User>().AddAsync(tempUser);

                // Id đã được sinh sẵn ở BaseEntity nên dùng được trước khi Save —
                // ghi user + lời mời trong CÙNG 1 SaveChanges để không bao giờ có user mồ côi
                var pendingInvitation = new TeamInvitation
                {
                    TeamId = request.TeamId,
                    InvitedUserId = tempUser.Id,
                    InvitedByUserId = currentUserId,
                    Status = TeamInvitationStatus.PendingAccept,
                    ExpiresAt = DateTime.UtcNow.AddHours(INVITATION_EXPIRY_HOURS),
                    Notes = request.Model.Notes
                };
                await _unitOfWork.GetRepository<TeamInvitation>().AddAsync(pendingInvitation);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Link kích hoạt phải trỏ về trang FE /auth/verify-email (FE sẽ gọi API xác thực),
                // KHÔNG dùng ApiBaseUrl vì production không cấu hình key này (sẽ ra localhost)
                var verifyLink = $"{frontendUrl}/auth/verify-email?token={verificationToken}";
                var newUserBody = EmailTemplate.Render(
                    heading: "Kích hoạt tài khoản SEAL",
                    greetingName: tempUser.FullName,
                    introHtml: $"Bạn được mời tham gia đội <b>{team.Name}</b> trong cuộc thi <b>{eventName}</b>. Email của bạn chưa có tài khoản — hệ thống đã tạo sẵn một tài khoản tạm.",
                    calloutLabel: "3 bước để tham gia",
                    calloutHtml: "1. Bấm nút dưới để <b>kích hoạt tài khoản</b> và nhận mật khẩu tạm.<br>2. Đăng nhập, <b>cập nhật hồ sơ sinh viên</b> để được duyệt.<br>3. Vào mục lời mời để <b>chấp nhận</b> tham gia đội.",
                    calloutKind: EmailTemplate.Callout.Info,
                    ctaText: "Kích hoạt tài khoản",
                    ctaUrl: verifyLink,
                    noteHtml: $"Liên kết kích hoạt và lời mời vào đội đều hết hạn sau {INVITATION_EXPIRY_HOURS} giờ.",
                    showLoginHint: false);
                try { await _emailService.SendEmailAsync(invitedEmail, $"[SEAL] Kích hoạt tài khoản để tham gia đội {team.Name}", newUserBody); }
                catch { /* Bỏ qua lỗi SMTP — lời mời vẫn được tạo thành công */ }

                return new InviteTeamMemberResponseModel
                {
                    InvitationId = pendingInvitation.Id,
                    TeamId = team.Id,
                    InvitedUserId = tempUser.Id,
                    Status = pendingInvitation.Status.ToString(),
                    ExpiresAt = pendingInvitation.ExpiresAt,
                    IsNewTemporaryUser = true
                };
            }

            // 4.1. Không thể mời tài khoản Quản trị (Admin) làm thí sinh — Admin không tham gia đội thi.
            if (invitedUser.IsAdmin)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Không thể mời người này: đây là tài khoản Quản trị, không thể tham gia đội thi.");
            }

            // 5. User chưa thuộc đội khác trong sự kiện này (1 user / 1 team / 1 event)
            var alreadyInTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == invitedUser.Id
                   && er.EventId == team.EventId
                   && er.TeamId != null
                   && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                cancellationToken);
            if (alreadyInTeam)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng đã tham gia một đội khác trong sự kiện này.");
            }

            // 5.1. User không được có các vai trò quản lý (Judge, Mentor, EventCoordinator) trong cùng sự kiện
            var hasConflictRole = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == invitedUser.Id
                   && er.EventId == team.EventId
                   && (er.RoleName == EventRoleType.Judge 
                    || er.RoleName == EventRoleType.Mentor 
                    || er.RoleName == EventRoleType.EventCoordinator),
                cancellationToken);

            if (hasConflictRole)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng đã là Giám khảo, Mentor hoặc Điều phối viên trong sự kiện này, không thể tham gia đội thi.");
            }

            // 6. User chưa có lời mời đang chờ (còn hiệu lực) trong sự kiện này
            var now = DateTime.UtcNow;
            var hasPendingInvite = await _unitOfWork.GetRepository<TeamInvitation>().Entities
                .AnyAsync(inv => inv.InvitedUserId == invitedUser.Id
                              && inv.Status == TeamInvitationStatus.PendingAccept
                              && inv.ExpiresAt > now
                              && inv.Team.EventId == team.EventId,
                          cancellationToken);
            if (hasPendingInvite)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng đang có một lời mời chờ phản hồi trong sự kiện này.");
            }

            // 7. Đội chưa đầy
            var currentMemberCount = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == request.TeamId
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .CountAsync(cancellationToken);
            if (currentMemberCount >= MAX_TEAM_SIZE)
            {
                return BaseException.BadRequestInvaildInputResponse($"Đội đã đủ tối đa {MAX_TEAM_SIZE} thành viên.");
            }

            // 8. Tạo lời mời PENDING_ACCEPT, hết hạn sau INVITATION_EXPIRY_HOURS giờ
            var invitation = new TeamInvitation
            {
                TeamId = request.TeamId,
                InvitedUserId = invitedUser.Id,
                InvitedByUserId = currentUserId,
                Status = TeamInvitationStatus.PendingAccept,
                ExpiresAt = now.AddHours(INVITATION_EXPIRY_HOURS),
                Notes = request.Model.Notes
            };

            // Người được mời là tài khoản TẠM chưa kích hoạt (tạo từ lời mời trước đó nhưng chưa bấm link):
            // cấp lại token kích hoạt mới — họ chưa có mật khẩu nên email "đăng nhập để chấp nhận" là vô dụng
            bool needsActivation = invitedUser.IsTemporary && !invitedUser.IsEmailVerified;
            if (needsActivation)
            {
                invitedUser.EmailVerificationToken = Guid.NewGuid().ToString();
                invitedUser.EmailVerificationExpiry = now.AddHours(ACTIVATION_EXPIRY_HOURS);
                await _unitOfWork.GetRepository<User>().UpdateAsync(invitedUser);
            }

            await _unitOfWork.GetRepository<TeamInvitation>().AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (needsActivation)
            {
                var reactivateLink = $"{frontendUrl}/auth/verify-email?token={invitedUser.EmailVerificationToken}";
                var reactivateBody = EmailTemplate.Render(
                    heading: "Kích hoạt tài khoản SEAL",
                    greetingName: invitedUser.FullName,
                    introHtml: $"Bạn được mời tham gia đội <b>{team.Name}</b> trong cuộc thi <b>{eventName}</b>. Tài khoản của bạn chưa được kích hoạt — hệ thống đã gửi lại link kích hoạt mới.",
                    calloutLabel: "3 bước để tham gia",
                    calloutHtml: "1. Bấm nút dưới để <b>kích hoạt tài khoản</b> và nhận mật khẩu tạm.<br>2. Đăng nhập, <b>cập nhật hồ sơ sinh viên</b> để được duyệt.<br>3. Vào mục lời mời để <b>chấp nhận</b> tham gia đội.",
                    calloutKind: EmailTemplate.Callout.Info,
                    ctaText: "Kích hoạt tài khoản",
                    ctaUrl: reactivateLink,
                    noteHtml: $"Liên kết kích hoạt và lời mời vào đội đều hết hạn sau {INVITATION_EXPIRY_HOURS} giờ.",
                    showLoginHint: false);
                try { await _emailService.SendEmailAsync(invitedUser.Email, $"[SEAL] Kích hoạt tài khoản để tham gia đội {team.Name}", reactivateBody); }
                catch { /* Bỏ qua lỗi SMTP — lời mời vẫn được tạo thành công */ }

                return new InviteTeamMemberResponseModel
                {
                    InvitationId = invitation.Id,
                    TeamId = invitation.TeamId,
                    InvitedUserId = invitation.InvitedUserId,
                    Status = invitation.Status.ToString(),
                    ExpiresAt = invitation.ExpiresAt,
                    IsNewTemporaryUser = true
                };
            }

            // Gửi email mời tham gia đội (đã có tài khoản)
            var teamLink = $"{frontendUrl}/teams/{team.Id}";
            var userEmailBody = EmailTemplate.Render(
                heading: "Lời mời tham gia đội",
                greetingName: invitedUser.FullName,
                introHtml: $"Bạn được mời tham gia đội <b>{team.Name}</b> trong cuộc thi <b>{eventName}</b>.",
                calloutLabel: "Thông tin lời mời",
                calloutHtml: $"Đội: <b>{team.Name}</b><br>Sự kiện: <b>{eventName}</b>",
                calloutKind: EmailTemplate.Callout.Success,
                ctaText: "Xem lời mời",
                ctaUrl: teamLink,
                ctaFallbackUrl: frontendUrl,
                noteHtml: $"Đăng nhập để chấp nhận hoặc từ chối. Lời mời sẽ hết hạn sau {INVITATION_EXPIRY_HOURS} giờ.");

            try { await _emailService.SendEmailAsync(invitedUser.Email, $"[SEAL] Lời mời tham gia đội {team.Name}", userEmailBody); }
            catch { /* Bỏ qua lỗi SMTP — lời mời vẫn được tạo thành công, tránh trả Network Error cho FE */ }

            return new InviteTeamMemberResponseModel
            {
                InvitationId = invitation.Id,
                TeamId = invitation.TeamId,
                InvitedUserId = invitation.InvitedUserId,
                Status = invitation.Status.ToString(),
                ExpiresAt = invitation.ExpiresAt
            };
        }
    }
}


