using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        // Liên kết đặt lại mật khẩu hết hạn sau 24 giờ (đồng bộ với mọi email khác của hệ thống).
        private const int TOKEN_EXPIRY_HOURS = 24;
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(TOKEN_EXPIRY_HOURS);
        // Chống spam: mỗi tài khoản chỉ gửi lại 1 yêu cầu trong 5 phút.
        private static readonly TimeSpan RequestCooldown = TimeSpan.FromMinutes(5);

        public ForgotPasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _configuration = configuration;
            _memoryCache = memoryCache;
        }

        public async Task<Result<ForgotPasswordResponseModel>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            // Luôn trả cùng một thông báo chung để không lộ email nào tồn tại (chống dò email).
            var genericResponse = new ForgotPasswordResponseModel
            {
                Message = "Nếu email hợp lệ, chúng tôi đã gửi liên kết đặt lại mật khẩu. Vui lòng kiểm tra hộp thư."
            };

            var email = request.Model.Email?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                return genericResponse;
            }
            var emailLower = email.ToLower();

            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);

            // Chỉ xử lý tài khoản đã xác thực email — tránh ghi đè token đang dùng cho luồng
            // xác thực email của tài khoản mới / tài khoản tạm (2 luồng dùng chung field token).
            if (user == null || !user.IsEmailVerified)
            {
                return genericResponse;
            }

            // Chống spam theo email.
            var cooldownKey = $"ForgotPassword_{user.Id}";
            if (_memoryCache.TryGetValue(cooldownKey, out _))
            {
                return genericResponse;
            }

            // Tái dùng EmailVerificationToken/Expiry làm token đặt lại mật khẩu (không đổi schema).
            var resetToken = Guid.NewGuid().ToString("N");
            user.EmailVerificationToken = resetToken;
            user.EmailVerificationExpiry = DateTime.UtcNow.Add(TokenLifetime);
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _memoryCache.Set(cooldownKey, true, RequestCooldown);

            var frontendUrl = (_configuration["FrontendUrl"] ?? "https://swp391-frontend.vercel.app").TrimEnd('/');
            var resetLink = $"{frontendUrl}/auth/reset-password?token={resetToken}";
            var body =
                $"<h3>Chào {user.FullName},</h3>" +
                $"<p>Bạn (hoặc ai đó) đã yêu cầu đặt lại mật khẩu cho tài khoản <b>{user.Email}</b>.</p>" +
                $"<p>Nhấn vào liên kết dưới đây để đặt lại mật khẩu (hết hạn sau {TOKEN_EXPIRY_HOURS} giờ):</p>" +
                $"<p><a href='{resetLink}'>{resetLink}</a></p>" +
                $"<p>Nếu bạn không yêu cầu điều này, hãy bỏ qua email — mật khẩu của bạn không thay đổi.</p>";
            try
            {
                await _emailService.SendEmailAsync(user.Email, "[SEAL] Đặt lại mật khẩu", body);
            }
            catch
            {
                // Demo: không có SMTP thì bỏ qua lỗi gửi mail (token đã lưu, có thể dùng link trực tiếp).
            }

            return genericResponse;
        }
    }
}

