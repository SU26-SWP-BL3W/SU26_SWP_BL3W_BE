using Google.Apis.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SEAL_Application.Features.Users.Commands.LoginUser.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.GoogleLogin
{
    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<LoginUserResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings,
            IConfiguration config,
            IEmailService emailService,
            ILogger<GoogleLoginCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _config = config;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result<LoginUserResponseModel>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var idToken = request.Model.IdToken?.Trim();
            if (string.IsNullOrEmpty(idToken))
            {
                return BaseException.BadRequestInvaildInputResponse("Thiếu Google idToken.");
            }

            // 1. Xác thực idToken với Google: kiểm chữ ký của Google + audience PHẢI là ClientId của mình
            //    (chống dùng idToken của app Google khác). BẮT BUỘC có ClientId — thiếu config thì CHẶN
            //    đăng nhập (fail-closed), tuyệt đối không bỏ qua bước kiểm audience (tránh auth bypass).
            var clientId = _config["GoogleAuth:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Hệ thống chưa cấu hình GoogleAuth:ClientId nên không thể xác thực đăng nhập Google an toàn.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId } // LUÔN kiểm audience
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch
            {
                return BaseException.BadRequestInvaildInputResponse("Google idToken không hợp lệ hoặc đã hết hạn.");
            }

            if (payload == null || string.IsNullOrEmpty(payload.Email))
            {
                return BaseException.BadRequestInvaildInputResponse("Không lấy được email từ Google.");
            }

            var emailLower = payload.Email.ToLower();

            // 2. Tìm user theo email; đăng nhập Google lần đầu thì tự tạo tài khoản.
            var user = await _unitOfWork.GetRepository<User>().GetQueryable()
                .SingleOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);

            var isNewUser = user == null;

            if (user == null)
            {
                user = new User
                {
                    // SchoolId mặc định là "" (string.Empty) -> vi phạm FK_Users_Schools_SchoolId.
                    // Đặt null (chưa có trường) để FK cho phép; SV chọn trường khi nộp hồ sơ.
                    SchoolId = null,
                    StudentCode = null,
                    Email = payload.Email,
                    FullName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                    // Email đã được Google xác thực; mật khẩu đặt ngẫu nhiên (không dùng để đăng nhập thường).
                    PasswordHash = FixedSaltPasswordHasher.HashPassword(Guid.NewGuid().ToString("N")),
                    IsEmailVerified = true,
                    IsStudent = true,
                    IsAdmin = false,
                    IsApproved = false,
                    IsTemporary = false,
                    IsFpt = false,
                    PhotoStudentCardUrl = payload.Picture
                };
                await _unitOfWork.GetRepository<User>().AddAsync(user);
            }

            // 3. Sinh JWT (giống đăng nhập thường).
            var roles = new List<string>();
            if (user.IsAdmin) roles.Add("Admin");
            if (user.IsStudent) roles.Add("Student");

            var accessToken = _tokenService.GenerateAccessToken(user.Id, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            // Chỉ Update khi là tài khoản CŨ. Tài khoản mới đã được AddAsync (state = Added);
            // nếu gọi Update sẽ đổi sang Modified -> EF chạy UPDATE dòng chưa tồn tại
            // -> DbUpdateConcurrencyException. RefreshToken set ở trên vẫn được INSERT cùng.
            if (!isNewUser)
            {
                _unitOfWork.GetRepository<User>().Update(user);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Gửi email thông báo (bọc try/catch: gửi mail lỗi KHÔNG được làm hỏng đăng nhập).
            //    - Tài khoản mới (Google login lần đầu): mail chào mừng.
            //    - Đăng nhập lại: mail cảnh báo bảo mật "có đăng nhập Google mới".
            //    => mỗi lần đăng nhập chỉ 1 mail, tránh spam 2 mail cùng lúc ở lần đầu.
            try
            {
                string subject;
                string body;
                if (isNewUser)
                {
                    subject = "[SEAL] Chào mừng bạn đến với SEAL";
                    body =
                        $"<h3>Chào {user.FullName},</h3>" +
                        $"<p>Tài khoản SEAL của bạn vừa được tạo bằng đăng nhập Google với email <b>{user.Email}</b>.</p>" +
                        $"<p>Email đã được Google xác thực nên bạn có thể đăng nhập bất cứ lúc nào bằng nút " +
                        $"\"Đăng nhập bằng Google\". Để tham gia cuộc thi, vui lòng hoàn tất hồ sơ thí sinh và chờ ban tổ chức duyệt.</p>" +
                        $"<p>Nếu không phải bạn thực hiện, vui lòng liên hệ ban tổ chức ngay.</p>";
                }
                else
                {
                    subject = "[SEAL] Có đăng nhập Google mới vào tài khoản của bạn";
                    body =
                        $"<h3>Chào {user.FullName},</h3>" +
                        $"<p>Tài khoản <b>{user.Email}</b> vừa được đăng nhập bằng Google lúc " +
                        $"{CoreHelper.SystemTimeNow:HH:mm 'ngày' dd/MM/yyyy}.</p>" +
                        $"<p>Nếu đây là bạn, bạn có thể bỏ qua email này.</p>" +
                        $"<p>Nếu <b>không phải bạn</b>, vui lòng liên hệ ban tổ chức để được hỗ trợ khoá tài khoản.</p>";
                }
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                // nuốt lỗi gửi mail (SMTP lỗi/chưa cấu hình) — đăng nhập vẫn thành công.
                _logger.LogWarning(ex, "Gửi email thông báo Google Login thất bại cho {Email}", user.Email);
            }

            return new LoginUserResponseModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsAdmin = user.IsAdmin,
                IsStudent = user.IsStudent,
                MustChangePassword = user.MustChangePassword
            };
        }
    }
}

