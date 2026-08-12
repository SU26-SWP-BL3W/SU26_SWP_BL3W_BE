using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Features.Users.Commands.CreateUser.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserModel>>
    {
        // Thời hạn link xác thực email (giờ) — dùng chung cho cả lúc tạo token lẫn nội dung email
        private const int ACTIVATION_EXPIRY_HOURS = 24;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEmailService _emailService;
        private readonly string? _fptMockBaseUrl;
        private readonly string? _fptMockApiKey;
        private readonly string _frontendUrl;

        public RegisterUserCommandHandler(
            IUnitOfWork unitOfWork, 
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
            _fptMockBaseUrl = configuration["FptMockApi:BaseUrl"];
            _fptMockApiKey = configuration["FptMockApi:ApiKey"];
            _frontendUrl = (configuration["FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task<Result<UserModel>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra email đã tồn tại chưa
            var existingUser = await _unitOfWork.GetRepository<User>().GetQueryable()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Model.Email.ToLower(), cancellationToken);
            // Ngoại lệ: tài khoản TẠM chưa kích hoạt (được tạo khi có người mời email này vào đội)
            // -> cho phép chủ nhân thật của email đăng ký "nhận lại" tài khoản, giữ nguyên Id
            // để lời mời đang chờ không bị mất. Các trường hợp còn lại vẫn báo trùng email.
            bool claimTempAccount = existingUser != null && existingUser.IsTemporary && !existingUser.IsEmailVerified;
            if (existingUser != null && !claimTempAccount)
            {
                return BaseException.BadRequestDupplicationResponse($"Email '{request.Model.Email}' đã tồn tại trong hệ thống.");
            }

            string fullName = request.Model.FullName;

            // 3. Tạo token xác thực email
            var emailVerificationToken = Guid.NewGuid().ToString();
            var emailVerificationExpiry = DateTime.UtcNow.AddHours(ACTIVATION_EXPIRY_HOURS);

            User user;
            if (claimTempAccount)
            {
                // 4a. Nhận lại tài khoản tạm: cập nhật thông tin đăng ký thật + cấp token xác thực mới
                user = existingUser!;
                user.PasswordHash = FixedSaltPasswordHasher.HashPassword(request.Model.Password);
                user.FullName = fullName;
                user.IsTemporary = false;
                user.EmailVerificationToken = emailVerificationToken;
                user.EmailVerificationExpiry = emailVerificationExpiry;
                await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            }
            else
            {
                // 4b. Tạo thực thể User mới
                user = new User
                {
                    SchoolId = null,
                    StudentCode = null,
                    Email = request.Model.Email,
                    PasswordHash = FixedSaltPasswordHasher.HashPassword(request.Model.Password),
                    FullName = fullName,
                    IsStudent = false,
                    IsAdmin = false, // Không cho phép tự đăng ký làm Admin
                    IsApproved = false, // Sẽ được approve sau khi xác thực email thành công
                    IsEmailVerified = false,
                    EmailVerificationToken = emailVerificationToken,
                    EmailVerificationExpiry = emailVerificationExpiry,
                    IsFpt = false,
                    PhotoStudentCardUrl = null
                };
                await _unitOfWork.GetRepository<User>().AddAsync(user);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Gửi email xác thực
            // Link kích hoạt phải trỏ về trang FE /auth/verify-email (FE sẽ gọi API xác thực),
            // KHÔNG dùng ApiBaseUrl vì production không cấu hình key này (sẽ ra localhost)
            var verificationLink = $"{_frontendUrl}/auth/verify-email?token={emailVerificationToken}";
            var emailBody = EmailTemplate.Render(
                heading: "Kích hoạt tài khoản SEAL",
                greetingName: user.FullName,
                introHtml: "Cảm ơn bạn đã đăng ký tài khoản trên <b>SEAL</b>. Chỉ còn một bước nữa là xong — hãy xác thực email để kích hoạt tài khoản.",
                calloutLabel: "2 bước để bắt đầu",
                calloutHtml: "1. Bấm nút dưới để <b>kích hoạt tài khoản</b>.<br>2. Đăng nhập rồi <b>cập nhật hồ sơ sinh viên</b> để được duyệt tham gia sự kiện.",
                calloutKind: EmailTemplate.Callout.Info,
                ctaText: "Kích hoạt tài khoản",
                ctaUrl: verificationLink,
                noteHtml: $"Liên kết kích hoạt sẽ hết hạn sau {ACTIVATION_EXPIRY_HOURS} giờ.",
                showLoginHint: false);

            await _emailService.SendEmailAsync(user.Email, "[SEAL] Kích hoạt tài khoản đăng ký", emailBody);

            return new UserModel
            {
                Id = user.Id,
                SchoolId = user.SchoolId ?? string.Empty,
                StudentCode = user.StudentCode,
                Email = user.Email,
                FullName = user.FullName,
                IsStudent = user.IsStudent,
                IsAdmin = user.IsAdmin,
                IsApproved = user.IsApproved,
                IsFpt = user.IsFpt,
                PhotoStudentCardUrl = user.PhotoStudentCardUrl
            };
        }

        private static string NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Replace(" ", "").ToLower();
        }
    }
}

