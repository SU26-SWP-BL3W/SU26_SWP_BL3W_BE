using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerifyEmailCommandHandler> _logger;
        private readonly string _frontendUrl;

        public VerifyEmailCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<VerifyEmailCommandHandler> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            var users = await _unitOfWork.GetRepository<User>().FindAsync(u => u.EmailVerificationToken == request.Token);
            var user = users.FirstOrDefault();

            if (user == null || user.EmailVerificationExpiry < DateTime.UtcNow)
            {
                return BaseException.BadRequestInvaildInputResponse("Token không hợp lệ hoặc đã hết hạn.");
            }

            user.IsEmailVerified = true;
            // tại sao lại null EmailVerificationToken , EmailVerificationExpiry
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;

            // Tài khoản tạm (giám khảo được mời): cấp mật khẩu tạm ngay khi xác thực email thành công và tự động duyệt
            string? tempPassword = null;
            if (user.IsTemporary)
            {
                user.IsApproved = true;
                tempPassword = TempPasswordGenerator.Generate();
                user.PasswordHash = FixedSaltPasswordHasher.HashPassword(tempPassword);
                user.MustChangePassword = true;
            }

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Gửi email chứa tài khoản đăng nhập tạm thời (chỉ với tài khoản tạm)
            if (tempPassword != null)
            {
                // Tài khoản tạm do MỜI VÀO ĐỘI tạo ra (đang có lời mời chờ) -> nội dung khác giám khảo
                var isMemberInvite = await _unitOfWork.GetRepository<TeamInvitation>().AnyAsync(
                    i => i.InvitedUserId == user.Id && i.Status == TeamInvitationStatus.PendingAccept,
                    cancellationToken);

                // Nút đăng nhập trỏ về trang FE /auth (KHÔNG dùng ApiBaseUrl — production không cấu hình key đó)
                var loginLink = $"{_frontendUrl}/login";
                var taiKhoanHtml = $"Email: <b>{user.Email}</b><br>Mật khẩu tạm: <b>{tempPassword}</b>";

                var subject = "[SEAL] Kích hoạt tài khoản thành công";
                string body;
                if (isMemberInvite)
                {
                    body = EmailTemplate.Render(
                        heading: "Kích hoạt tài khoản thành công",
                        greetingName: user.FullName,
                        introHtml: "Tài khoản của bạn đã được kích hoạt. Dùng thông tin bên dưới để đăng nhập hệ thống <b>SEAL</b>.",
                        calloutLabel: "Tài khoản đăng nhập",
                        calloutHtml: taiKhoanHtml,
                        calloutKind: EmailTemplate.Callout.Success,
                        ctaText: "Đăng nhập ngay",
                        ctaUrl: loginLink,
                        noteHtml: "Sau khi đăng nhập, hãy <b>cập nhật hồ sơ sinh viên</b> để được duyệt, rồi vào mục <b>lời mời</b> để chấp nhận tham gia đội.",
                        showLoginHint: false);
                }
                else
                {
                    body = EmailTemplate.Render(
                        heading: "Kích hoạt tài khoản thành công",
                        greetingName: user.FullName,
                        introHtml: "Email của bạn đã được xác thực. Dùng tài khoản tạm bên dưới để đăng nhập và nhận vai trò được mời trong sự kiện.",
                        calloutLabel: "Tài khoản đăng nhập",
                        calloutHtml: taiKhoanHtml,
                        calloutKind: EmailTemplate.Callout.Success,
                        ctaText: "Đăng nhập ngay",
                        ctaUrl: loginLink,
                        noteHtml: "Sau khi đăng nhập, mở mục <b>lời mời</b> để phản hồi. Tài khoản tạm chỉ có hiệu lực trong thời gian diễn ra sự kiện.",
                        showLoginHint: false);
                }
                try { await _emailService.SendEmailAsync(user.Email, subject, body); }
                catch (Exception ex) { _logger.LogWarning(ex, "Gửi email mật khẩu tạm thất bại cho {Email}", user.Email); }
            }

            return true;
        }
    }
}

