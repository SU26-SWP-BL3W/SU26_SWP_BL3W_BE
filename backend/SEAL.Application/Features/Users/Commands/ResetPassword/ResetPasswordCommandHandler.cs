using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<ResetPasswordCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var token = request.Model.Token?.Trim();
            if (string.IsNullOrEmpty(token))
            {
                return BaseException.BadRequestInvaildInputResponse("Token không hợp lệ hoặc đã hết hạn.");
            }

            // Token đặt lại mật khẩu tái dùng field EmailVerificationToken/Expiry.
            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);

            if (user == null || user.EmailVerificationExpiry == null || user.EmailVerificationExpiry < DateTime.UtcNow)
            {
                return BaseException.BadRequestInvaildInputResponse("Token không hợp lệ hoặc đã hết hạn.");
            }

            // Đổi mật khẩu + vô hiệu token (dùng 1 lần). Dùng được link reset cũng đồng nghĩa đã sở hữu email.
            user.PasswordHash = FixedSaltPasswordHasher.HashPassword(request.Model.NewPassword);
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;

            // #2 Vô hiệu các phiên đăng nhập cũ: đổi mật khẩu thì refresh token cũ hết giá trị
            //     (nếu tài khoản bị chiếm, kẻ trộm mất luôn phiên đang đăng nhập).
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // #1 Gửi mail CẢNH BÁO mật khẩu vừa được đổi (không chặn luồng nếu SMTP lỗi).
            try
            {
                var body = $"<h3>Chào {user.FullName},</h3>" +
                           $"<p>Mật khẩu của tài khoản <b>{user.Email}</b> vừa được thay đổi.</p>" +
                           "<p>Nếu <b>không phải bạn</b> thực hiện, hãy liên hệ ban tổ chức ngay để bảo vệ tài khoản.</p>";
                await _emailService.SendEmailAsync(user.Email, "[SEAL] Mật khẩu của bạn vừa được thay đổi", body);
            }
            catch (Exception ex)
            {
                // SMTP lỗi thì bỏ qua — việc đổi mật khẩu đã thành công, không để mail làm hỏng luồng.
                _logger.LogWarning(ex, "Gửi email cảnh báo đổi mật khẩu thất bại cho {Email}", user.Email);
            }

            return true;
        }
    }
}

