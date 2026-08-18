using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Commands.RequestUnblock.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.RequestUnblock
{
    /// <summary>
    /// User bị chặn cập nhật hồ sơ (bị từ chối >= 2 lần) tự gửi yêu cầu xin được nộp lại.
    /// Hệ thống gửi mail cho ban tổ chức (kèm lịch sử từ chối) + mail xác nhận cho user.
    /// Ban tổ chức gỡ chặn bằng cách xóa bớt 1 bản ghi UserRejection (DeleteUserRejection) để count tụt < 2.
    /// </summary>
    public class RequestUnblockCommandHandler : IRequestHandler<RequestUnblockCommand, Result<RequestUnblockResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RequestUnblockCommandHandler> _logger;

        // Ngưỡng bị chặn cập nhật hồ sơ - đồng bộ với UpdateStudentProfileCommandHandler.
        private const int RejectionLockThreshold = 2;
        // Chống spam: mỗi tài khoản chỉ gửi 1 yêu cầu trong 24h.
        private static readonly TimeSpan RequestCooldown = TimeSpan.FromHours(24);

        public RequestUnblockCommandHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IConfiguration configuration,
            IMemoryCache memoryCache,
            ILogger<RequestUnblockCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<Result<RequestUnblockResponseModel>> Handle(RequestUnblockCommand request, CancellationToken cancellationToken)
        {
            // Luôn trả về CÙNG một thông báo chung để không lộ email nào tồn tại / đang bị khóa (chống dò email).
            var genericResponse = new RequestUnblockResponseModel
            {
                Message = "Nếu tài khoản hợp lệ và đang bị khóa, yêu cầu gỡ khóa đã được gửi tới ban tổ chức. Vui lòng theo dõi email để nhận phản hồi."
            };

            var email = request.Model.Email?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                return genericResponse;
            }
            var emailLower = email.ToLower();

            // 1. Tìm user theo email
            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);
            if (user == null)
            {
                return genericResponse; // không lộ việc email không tồn tại
            }

            // 2. Chỉ xử lý nếu tài khoản thực sự đang bị khóa (>= 2 lần từ chối)
            var rejections = await _unitOfWork.GetRepository<UserRejection>().Entities
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedTime)
                .ToListAsync(cancellationToken);
            if (rejections.Count < RejectionLockThreshold)
            {
                return genericResponse; // chưa bị khóa thì không gửi gì
            }

            // 3. Chống spam: mỗi tài khoản chỉ 1 yêu cầu / 24h
            var cacheKey = $"unblock-request:{user.Id}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return genericResponse; // đã gửi gần đây, bỏ qua
            }
            _memoryCache.Set(cacheKey, true, RequestCooldown);

            // 4. Gửi mail cho BTC + mail xác nhận cho user. Bọc try/catch để lỗi gửi mail không làm fail API.
            try
            {
                var supportEmail = _configuration["SupportEmail"] ?? "support@seal.edu.vn";
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://swp391-frontend.vercel.app";

                await _emailService.SendEmailAsync(
                    supportEmail,
                    $"[SEAL] Yêu cầu gỡ khóa tài khoản: {user.Email}",
                    BuildAdminEmail(user, rejections, frontendUrl));

                await _emailService.SendEmailAsync(
                    user.Email,
                    "[SEAL] Đã nhận yêu cầu gỡ khóa tài khoản",
                    BuildUserEmail(user, supportEmail));
            }
            catch (Exception ex)
            {
                // Bỏ qua lỗi gửi email — vẫn trả về thông báo chung.
                _logger.LogWarning(ex, "Gửi email yêu cầu gỡ khóa thất bại cho {Email}", user.Email);
            }

            return genericResponse;
        }

        private static string BuildAdminEmail(User user, List<UserRejection> rejections, string frontendUrl)
        {
            var safeName = WebUtility.HtmlEncode(user.FullName);
            var safeEmail = WebUtility.HtmlEncode(user.Email);
            var total = rejections.Count;
            var reasonItems = string.Join("", rejections.Select((r, i) =>
                $"<div style=\"margin-bottom:6px;\"><b>Lần {total - i}:</b> {WebUtility.HtmlEncode(r.Reason ?? string.Empty)}</div>"));
            // Trang quản trị người dùng (không có route /admin — dùng /users, nơi admin xem & xử lý tài khoản).
            var adminLink = $"{frontendUrl}/admin/users";

            return EmailTemplate.Render(
                heading: "Có yêu cầu gỡ khóa tài khoản",
                greetingName: null,
                introHtml: $"Người dùng dưới đây đã bị chặn cập nhật hồ sơ do bị từ chối <b>{total} lần</b> và vừa gửi yêu cầu được hỗ trợ nộp lại:<br><b>Họ tên:</b> {safeName}<br><b>Email:</b> {safeEmail}",
                calloutLabel: "Lịch sử từ chối",
                calloutHtml: reasonItems,
                calloutKind: EmailTemplate.Callout.Danger,
                ctaText: "Mở trang quản trị",
                ctaUrl: adminLink,
                showLoginHint: false,
                noteHtml: "Để gỡ khóa: đăng nhập trang quản trị, tìm người dùng theo email ở trên rồi xóa bớt 1 bản ghi từ chối để số lần còn dưới 2.");
        }

        private static string BuildUserEmail(User user, string supportEmail)
        {
            var safeName = WebUtility.HtmlEncode(user.FullName);
            var safeSupport = WebUtility.HtmlEncode(supportEmail);
            return EmailTemplate.Render(
                heading: "Đã nhận yêu cầu gỡ khóa của bạn",
                greetingName: safeName,
                introHtml: "Chúng tôi đã nhận được yêu cầu gỡ khóa tài khoản của bạn. Ban tổ chức sẽ xem xét lịch sử hồ sơ và phản hồi qua email trong thời gian sớm nhất.",
                showLoginHint: false,
                noteHtml: $"Nếu cần hỗ trợ gấp, vui lòng liên hệ: <a href=\"mailto:{safeSupport}\" style=\"color:#2dd4bf;\">{safeSupport}</a>");
        }
    }
}

