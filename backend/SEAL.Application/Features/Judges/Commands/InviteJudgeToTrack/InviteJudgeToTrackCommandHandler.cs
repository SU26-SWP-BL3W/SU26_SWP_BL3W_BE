using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventRoles;
using SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack
{
    /// <summary>
    /// Mời một giám khảo (đã có tài khoản) chấm đúng MỘT hạng mục (Track).
    /// KHÔNG tạo EventRole ngay — chỉ tạo lời mời (EventRoleInvitation) chờ phản hồi và gửi email kèm
    /// link Đồng ý/Từ chối. EventRole (Judge gắn Track) chỉ được tạo khi giám khảo bấm chấp nhận.
    /// </summary>
    public class InviteJudgeToTrackCommandHandler : IRequestHandler<InviteJudgeToTrackCommand, Result<InviteJudgeToTrackResponseModel>>
    {
        private const int INVITATION_EXPIRY_HOURS = 24;
        // Thời hạn link kích hoạt tài khoản tạm (giờ) — dùng chung cho cả lúc tạo token lẫn nội dung email
        private const int ACTIVATION_EXPIRY_HOURS = 24;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<InviteJudgeToTrackCommandHandler> _logger;
        private readonly string _frontendUrl;

        public InviteJudgeToTrackCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            INotificationService notificationService,
            ILogger<InviteJudgeToTrackCommandHandler> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task<Result<InviteJudgeToTrackResponseModel>> Handle(InviteJudgeToTrackCommand request, CancellationToken cancellationToken)
        {
            var m = request.Model;
            var invitedByUserId = _currentUserService.UserId;

            // 1. Sự kiện phải tồn tại
            var ev = await _unitOfWork.GetRepository<Event>().GetByIdAsync(m.EventId);
            if (ev == null)
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{m.EventId}' không tồn tại.");

            // 2. Track phải tồn tại và thuộc đúng sự kiện
            var track = await _unitOfWork.GetRepository<Track>()
                .GetByIdAsync(m.TrackId);
            if (track == null)
                return BaseException.BadRequestNotFoundResponse($"Hạng mục (Track) có ID '{m.TrackId}' không tồn tại.");
            if (track.EventId != m.EventId)
                return BaseException.BadRequestResponse($"Hạng mục '{m.TrackId}' không thuộc sự kiện '{m.EventId}'.");

            var email = m.JudgeEmail.Trim();

            // 3. Người được mời: nếu CHƯA có tài khoản thì tạo tài khoản TẠM (IsTemporary) + gửi email xác thực
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
                    FullName = !string.IsNullOrWhiteSpace(m.JudgeFullName) ? m.JudgeFullName.Trim() : email.Split('@')[0],
                    IsTemporary = true,
                    EmailVerificationToken = verificationToken,
                    EmailVerificationExpiry = DateTime.UtcNow.AddHours(ACTIVATION_EXPIRY_HOURS)
                };
                await _unitOfWork.GetRepository<User>().AddAsync(invitedUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // lưu để User có Id trước khi tạo lời mời

                // Link kích hoạt phải trỏ về trang FE /auth/verify-email (FE sẽ gọi API xác thực),
                // KHÔNG dùng ApiBaseUrl vì production không cấu hình key này (sẽ ra localhost)
                var verificationLink = $"{_frontendUrl}/verify-email?token={verificationToken}";
                var verifyBody = EmailTemplate.Render(
                    heading: "Kích hoạt tài khoản SEAL",
                    greetingName: invitedUser.FullName,
                    introHtml: $"Bạn được mời làm <b>giám khảo</b> chấm hạng mục <b>{track.TrackName}</b> trong sự kiện <b>{ev.EventName}</b>. Email của bạn chưa có tài khoản — hệ thống đã tạo sẵn một tài khoản tạm.",
                    calloutLabel: "2 bước để bắt đầu",
                    calloutHtml: "1. Bấm nút dưới để <b>kích hoạt tài khoản</b> và nhận mật khẩu tạm.<br>2. Đăng nhập rồi mở mục <b>lời mời</b> để phản hồi.",
                    calloutKind: EmailTemplate.Callout.Info,
                    ctaText: "Kích hoạt tài khoản",
                    ctaUrl: verificationLink,
                    noteHtml: $"Liên kết kích hoạt sẽ hết hạn sau {ACTIVATION_EXPIRY_HOURS} giờ.",
                    showLoginHint: false);
                try { await _emailService.SendEmailAsync(invitedUser.Email, "[SEAL] Kích hoạt tài khoản để tham gia chấm thi", verifyBody); }
                catch (Exception ex) { _logger.LogWarning(ex, "Gửi email kích hoạt tài khoản tạm thất bại cho {Email}", invitedUser.Email); }
            }

            // 4. Kiểm tra xung đột vai trò qua EventRoleValidationHelper (đồng bộ với Assign/Update):
            //    trùng vai trò, EC⟷Giám khảo/Mentor, Giám khảo⟷Mentor cùng hạng mục, thí sinh không kiêm nhiệm.
            var roleConflict = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _unitOfWork, invitedUser.Id, m.EventId, EventRoleType.Judge, m.TrackId, null, cancellationToken);
            if (roleConflict != null)
                return BaseException.BadRequestDupplicationResponse(roleConflict);

            // 5. Không tạo trùng lời mời còn hiệu lực (cùng vai trò Judge cho hạng mục này)
            var now = DateTime.UtcNow;
            var hasPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                i => i.InvitedUserId == invitedUser.Id && i.EventId == m.EventId
                  && i.TrackId == m.TrackId && i.RoleName == EventRoleType.Judge
                  && i.Status == EventRoleInvitationStatus.Pending
                  && i.ExpiresAt > now,
                cancellationToken);
            if (hasPendingInvite)
                return BaseException.BadRequestInvaildInputResponse(
                    "Giám khảo đang có một lời mời chấm hạng mục này chờ phản hồi.");

            // 5b. Chặn NGAY TỪ LÚC MỜI: người này đang có lời mời vai trò XUNG KHẮC (EC/Mentor) chờ phản hồi
            //     trong cùng sự kiện -> không gửi email mâu thuẫn (lớp re-check khi accept vẫn giữ làm lưới an toàn).
            var hasConflictPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                i => i.InvitedUserId == invitedUser.Id && i.EventId == m.EventId
                  && (i.RoleName == EventRoleType.EventCoordinator || (i.RoleName == EventRoleType.Mentor && i.TrackId == m.TrackId))
                  && i.Status == EventRoleInvitationStatus.Pending
                  && i.ExpiresAt > now,
                cancellationToken);
            if (hasConflictPendingInvite)
                return BaseException.BadRequestInvaildInputResponse(
                    "Người này đang có lời mời làm Event Coordinator hoặc làm Cố vấn hạng mục này chờ phản hồi — không thể mời làm Giám khảo.");

            // 6. Tạo lời mời (Pending)
            var invitation = new EventRoleInvitation
            {
                EventId = m.EventId,
                TrackId = m.TrackId,
                InvitedUserId = invitedUser.Id,
                InvitedByUserId = invitedByUserId ?? string.Empty,
                RoleName = EventRoleType.Judge,
                Status = EventRoleInvitationStatus.Pending,
                ExpiresAt = now.AddHours(INVITATION_EXPIRY_HOURS),
                Notes = m.Notes
            };
            await _unitOfWork.GetRepository<EventRoleInvitation>().AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Gửi email kèm 2 nút Đồng ý / Từ chối
            bool invitationEmailSent = true;
            // Không dùng link accept/decline trực tiếp (GET link tự đổi state rất dễ bị prefetch/quét
            // bởi email scanner làm chấp nhận/từ chối nhầm) — trỏ về trang "Lời mời của tôi" (đã có sẵn,
            // xử lý được cả EventRole lẫn Team invitation) để người dùng tự bấm sau khi đăng nhập.
            var acceptLink = $"{_frontendUrl}/my-invitations";
            var declineLink = $"{_frontendUrl}/my-invitations";
            var subject = $"[SEAL] Mời chấm thi hạng mục '{track.TrackName}'";
            var body = EmailTemplate.Render(
                heading: "Lời mời chấm thi",
                greetingName: invitedUser.FullName,
                introHtml: $"Bạn được mời làm <b>giám khảo</b> chấm hạng mục <b>{track.TrackName}</b> trong sự kiện <b>{ev.EventName}</b>.",
                calloutLabel: "Thông tin lời mời",
                calloutHtml: $"Sự kiện: <b>{ev.EventName}</b><br>Hạng mục: <b>{track.TrackName}</b><br>Vai trò: <b>Giám khảo (Judge)</b>",
                calloutKind: EmailTemplate.Callout.Success,
                ctaText: "Đồng ý",
                ctaUrl: acceptLink,
                ctaText2: "Từ chối",
                ctaUrl2: declineLink,
                ctaFallbackUrl: $"{_frontendUrl}/my-invitations",
                noteHtml: $"Lời mời sẽ hết hạn sau {INVITATION_EXPIRY_HOURS} giờ. Nếu bạn không chấp nhận, vai trò sẽ không được tạo.");
            try
            {
                await _emailService.SendEmailAsync(invitedUser.Email, subject, body);
            }
            catch (Exception ex)
            {
                invitationEmailSent = false;
                _logger.LogWarning(ex, "Gửi email lời mời Giám khảo thất bại cho {Email}", invitedUser.Email);
            }

            // 8. Thông báo trong hệ thống cho người được mời (song song với email, không phụ thuộc email có gửi được không)
            await _notificationService.NotifyAsync(
                invitedUser.Id,
                "Lời mời làm Giám khảo",
                $"Bạn được mời làm Giám khảo chấm hạng mục '{track.TrackName}' trong sự kiện '{ev.EventName}'.",
                "staff_invite",
                $"/invitations/{invitation.Id}",
                cancellationToken);

            return new InviteJudgeToTrackResponseModel
            {
                InvitationId = invitation.Id,
                InvitedUserId = invitedUser.Id,
                JudgeEmail = invitedUser.Email,
                JudgeFullName = invitedUser.FullName,
                EventId = m.EventId,
                TrackId = m.TrackId,
                TrackName = track.TrackName,
                Status = invitation.Status.ToString(),
                ExpiresAt = invitation.ExpiresAt,
                InvitationEmailSent = invitationEmailSent
            };
        }
    }
}

