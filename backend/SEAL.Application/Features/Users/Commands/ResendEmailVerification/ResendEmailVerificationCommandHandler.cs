using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEAL_Application.Commons;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.ResendEmailVerification
{
    public class ResendEmailVerificationCommandHandler : IRequestHandler<ResendEmailVerificationCommand, Result<bool>>
    {
        private const int ACTIVATION_EXPIRY_HOURS = 24;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly string _frontendUrl;

        public ResendEmailVerificationCommandHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task<Result<bool>> Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLower();
            var user = await _unitOfWork.GetRepository<User>().GetQueryable()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

            // Không tiết lộ email có tồn tại hay không.
            if (user == null || user.IsEmailVerified)
            {
                return true;
            }

            user.EmailVerificationToken = Guid.NewGuid().ToString();
            user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(ACTIVATION_EXPIRY_HOURS);
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var verificationLink = $"{_frontendUrl}/auth/verify-email?token={user.EmailVerificationToken}";
            var emailBody = EmailTemplate.Render(
                heading: "Gửi lại liên kết kích hoạt SEAL",
                greetingName: user.FullName,
                introHtml: "Bạn (hoặc ai đó) vừa yêu cầu gửi lại email xác thực tài khoản <b>SEAL</b>.",
                calloutLabel: "Kích hoạt tài khoản",
                calloutHtml: "Bấm nút dưới để xác thực email. Nếu bạn không yêu cầu, hãy bỏ qua thư này.",
                calloutKind: EmailTemplate.Callout.Info,
                ctaText: "Kích hoạt tài khoản",
                ctaUrl: verificationLink,
                noteHtml: $"Liên kết sẽ hết hạn sau {ACTIVATION_EXPIRY_HOURS} giờ.",
                showLoginHint: false);

            try { await _emailService.SendEmailAsync(user.Email, "[SEAL] Gửi lại kích hoạt tài khoản", emailBody); }
            catch { }

            return true;
        }
    }
}
