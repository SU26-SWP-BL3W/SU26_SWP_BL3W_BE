using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventRoles;
using SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator
{
    /// <summary>
    /// Mời một người dùng (đã có tài khoản) làm Event Coordinator của sự kiện.
    /// KHÔNG tạo EventRole ngay — chỉ tạo lời mời (EventRoleInvitation) ở trạng thái chờ và gửi email
    /// kèm link Đồng ý/Từ chối. EventRole chỉ được tạo khi người được mời bấm chấp nhận.
    /// </summary>
    public class InviteEventCoordinatorCommandHandler : IRequestHandler<InviteEventCoordinatorCommand, Result<InviteEventCoordinatorResponseModel>>
    {
        private const int INVITATION_EXPIRY_HOURS = 24;
        // Thời hạn link kích hoạt tài khoản tạm (giờ) — dùng chung cho cả lúc tạo token lẫn nội dung email
        private const int ACTIVATION_EXPIRY_HOURS = 24;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly string _frontendUrl;

        public InviteEventCoordinatorCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task<Result<InviteEventCoordinatorResponseModel>> Handle(InviteEventCoordinatorCommand request, CancellationToken cancellationToken)
        {
            var m = request.Model;
            var invitedByUserId = _currentUserService.UserId;

            // 1. Sự kiện phải tồn tại
            var ev = await _unitOfWork.GetRepository<Event>().GetByIdAsync(m.EventId);
            if (ev == null)
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{m.EventId}' không tồn tại.");

            var email = m.CoordinatorEmail.Trim();

            // 2. Người được mời: nếu CHƯA có tài khoản thì tạo tài khoản TẠM (IsTemporary) + gửi email xác thực
            //    (thay vì báo lỗi). Đã có tài khoản thì dùng luôn.
            var invitedUser = await _unitOfWork.GetRepository<User>().GetQueryable()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
            if (invitedUser == null)
            {
                var verificationToken = Guid.NewGuid().ToString();
                invitedUser = new User
                {
                    Email = email,
                    SchoolId = null,   // BẮT BUỘC: mặc định entity là "" -> vi phạm FK_Users_Schools_SchoolId
                    FullName = email.Split('@')[0],
                    IsTemporary = true,
                    EmailVerificationToken = verificationToken,
                    EmailVerificationExpiry = DateTime.UtcNow.AddHours(ACTIVATION_EXPIRY_HOURS)
                };
                await _unitOfWork.GetRepository<User>().AddAsync(invitedUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // lưu để User có Id trước khi tạo lời mời

                // Link kích hoạt phải trỏ về trang FE /auth/verify-email (FE sẽ gọi API xác thực),
                // KHÔNG dùng ApiBaseUrl vì production không cấu hình key này (sẽ ra localhost)
                var verificationLink = $"{_frontendUrl}/auth/verify-email?token={verificationToken}";
                var verifyBody = EmailTemplate.Render(
                    heading: "Kích hoạt tài khoản SEAL",
                    greetingName: invitedUser.FullName,
                    introHtml: $"Bạn được mời tham gia <b>Ban tổ chức</b> sự kiện <b>{ev.EventName}</b>. Email của bạn chưa có tài khoản — hệ thống đã tạo sẵn một tài khoản tạm.",
                    calloutLabel: "2 bước để bắt đầu",
                    calloutHtml: "1. Bấm nút dưới để <b>kích hoạt tài khoản</b> và nhận mật khẩu tạm.<br>2. Đăng nhập rồi mở mục <b>lời mời</b> để phản hồi.",
                    calloutKind: EmailTemplate.Callout.Info,
                    ctaText: "Kích hoạt tài khoản",
                    ctaUrl: verificationLink,
                    noteHtml: $"Liên kết kích hoạt sẽ hết hạn sau {ACTIVATION_EXPIRY_HOURS} giờ.",
                    showLoginHint: false);
                try { await _emailService.SendEmailAsync(invitedUser.Email, "[SEAL] Kích hoạt tài khoản để tham gia Ban tổ chức", verifyBody); }
                catch { /* Bỏ qua lỗi gửi mail khi không có SMTP */ }
            }

            // 3. Kiểm tra xung đột vai trò qua EventRoleValidationHelper (đồng bộ với Assign/Update):
            //    trùng vai trò EC, EC⟷Giám khảo/Mentor, thí sinh không kiêm nhiệm.
            var roleConflict = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _unitOfWork, invitedUser.Id, m.EventId, EventRoleType.EventCoordinator, null, null, cancellationToken);
            if (roleConflict != null)
                return BaseException.BadRequestDupplicationResponse(roleConflict);

            // 4. Không tạo trùng lời mời còn hiệu lực
            var now = DateTime.UtcNow;
            var hasPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                i => i.InvitedUserId == invitedUser.Id && i.EventId == m.EventId
                  && i.RoleName == EventRoleType.EventCoordinator
                  && i.Status == EventRoleInvitationStatus.Pending
                  && i.ExpiresAt > now,
                cancellationToken);
            if (hasPendingInvite)
                return BaseException.BadRequestInvaildInputResponse(
                    "Người dùng đang có một lời mời làm Event Coordinator chờ phản hồi trong sự kiện này.");

            // 4b. Chặn NGAY TỪ LÚC MỜI: người này đang có lời mời vai trò XUNG KHẮC (Giám khảo/Mentor) chờ phản hồi
            //     trong cùng sự kiện -> không gửi email mâu thuẫn (lớp re-check khi accept vẫn giữ làm lưới an toàn).
            var hasConflictPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                i => i.InvitedUserId == invitedUser.Id && i.EventId == m.EventId
                  && (i.RoleName == EventRoleType.Judge || i.RoleName == EventRoleType.Mentor)
                  && i.Status == EventRoleInvitationStatus.Pending
                  && i.ExpiresAt > now,
                cancellationToken);
            if (hasConflictPendingInvite)
                return BaseException.BadRequestInvaildInputResponse(
                    "Người này đang có lời mời làm Giám khảo hoặc Cố vấn chờ phản hồi trong sự kiện — không thể mời làm Event Coordinator (các vai trò này không được kiêm nhiệm).");

            // 5. Tạo lời mời (Pending)
            var invitation = new EventRoleInvitation
            {
                EventId = m.EventId,
                TrackId = null,
                InvitedUserId = invitedUser.Id,
                InvitedByUserId = invitedByUserId ?? string.Empty,
                RoleName = EventRoleType.EventCoordinator,
                Status = EventRoleInvitationStatus.Pending,
                ExpiresAt = now.AddHours(INVITATION_EXPIRY_HOURS),
                Notes = m.Notes
            };
            await _unitOfWork.GetRepository<EventRoleInvitation>().AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Gửi email kèm 2 nút Đồng ý / Từ chối
            bool invitationEmailSent = true;
            var acceptLink = $"{_frontendUrl}/invitations/{invitation.Id}?action=accept";
            var declineLink = $"{_frontendUrl}/invitations/{invitation.Id}?action=decline";
            var subject = $"[SEAL] Mời làm Event Coordinator cho sự kiện '{ev.EventName}'";
            var body = EmailTemplate.Render(
                heading: "Lời mời làm Event Coordinator",
                greetingName: invitedUser.FullName,
                introHtml: $"Bạn được mời làm <b>Event Coordinator (Điều phối viên)</b> trong sự kiện <b>{ev.EventName}</b>.",
                calloutLabel: "Thông tin lời mời",
                calloutHtml: $"Sự kiện: <b>{ev.EventName}</b><br>Vai trò: <b>Event Coordinator</b>",
                calloutKind: EmailTemplate.Callout.Success,
                ctaText: "Đồng ý",
                ctaUrl: acceptLink,
                ctaText2: "Từ chối",
                ctaUrl2: declineLink,
                ctaFallbackUrl: $"{_frontendUrl}/invitations/{invitation.Id}",
                noteHtml: $"Lời mời sẽ hết hạn sau {INVITATION_EXPIRY_HOURS} giờ. Nếu bạn không chấp nhận, vai trò sẽ không được tạo.");
            try
            {
                await _emailService.SendEmailAsync(invitedUser.Email, subject, body);
            }
            catch
            {
                invitationEmailSent = false;
            }

            return new InviteEventCoordinatorResponseModel
            {
                InvitationId = invitation.Id,
                InvitedUserId = invitedUser.Id,
                CoordinatorEmail = invitedUser.Email,
                CoordinatorFullName = invitedUser.FullName,
                EventId = m.EventId,
                Status = invitation.Status.ToString(),
                ExpiresAt = invitation.ExpiresAt,
                InvitationEmailSent = invitationEmailSent
            };
        }
    }
}

