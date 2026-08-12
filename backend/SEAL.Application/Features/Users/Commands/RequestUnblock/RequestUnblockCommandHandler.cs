using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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

        // Ngưỡng bị chặn cập nhật hồ sơ - đồng bộ với UpdateStudentProfileCommandHandler.
        private const int RejectionLockThreshold = 2;
        // Chống spam: mỗi tài khoản chỉ gửi 1 yêu cầu trong 24h.
        private static readonly TimeSpan RequestCooldown = TimeSpan.FromHours(24);

        public RequestUnblockCommandHandler(
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
            catch
            {
                // Bỏ qua lỗi gửi email — vẫn trả về thông báo chung.
            }

            return genericResponse;
        }

        private static string BuildAdminEmail(User user, List<UserRejection> rejections, string frontendUrl)
        {
            var safeName = WebUtility.HtmlEncode(user.FullName);
            var safeEmail = WebUtility.HtmlEncode(user.Email);
            var total = rejections.Count;
            var reasonItems = string.Join("", rejections.Select((r, i) =>
                $"<li style=\"margin-bottom:6px;color:#374151;font-size:13px;line-height:1.5;\"><strong>Lần {total - i}:</strong> {WebUtility.HtmlEncode(r.Reason ?? string.Empty)}</li>"));
            // Trang quản trị người dùng (không có route /admin — dùng /users, nơi admin xem & xử lý tài khoản).
            var adminLink = $"{frontendUrl}/users";

            return
                "<div style=\"background:#f4f5f7;padding:24px 12px;font-family:Arial,Helvetica,sans-serif;\">" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\">" +
                "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background:#ffffff;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;\">" +
                "<tr><td style=\"background:#111418;padding:18px 32px;\">" +
                "<span style=\"color:#76b900;font-size:22px;font-weight:bold;letter-spacing:1px;\">SEAL</span>" +
                "<span style=\"color:#c9cdd2;font-size:12px;\"> &nbsp;·&nbsp; Yêu cầu gỡ khóa tài khoản</span>" +
                "</td></tr>" +
                "<tr><td style=\"padding:28px 32px 8px;\">" +
                "<h2 style=\"margin:0 0 14px;color:#111827;font-size:18px;\">Có yêu cầu gỡ khóa tài khoản</h2>" +
                $"<p style=\"margin:0 0 16px;color:#374151;font-size:14px;line-height:1.6;\">Người dùng dưới đây đã bị chặn cập nhật hồ sơ do bị từ chối <strong>{total} lần</strong> và vừa gửi yêu cầu được hỗ trợ nộp lại:</p>" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 18px;\"><tr>" +
                "<td style=\"background:#f9fafb;border:1px solid #e5e7eb;border-radius:6px;padding:14px 16px;\">" +
                $"<div style=\"color:#111827;font-size:14px;margin-bottom:4px;\"><strong>Họ tên:</strong> {safeName}</div>" +
                $"<div style=\"color:#111827;font-size:14px;\"><strong>Email:</strong> {safeEmail}</div>" +
                "</td></tr></table>" +
                "<div style=\"color:#991b1b;font-size:11px;font-weight:bold;letter-spacing:.5px;margin-bottom:6px;\">LỊCH SỬ TỪ CHỐI</div>" +
                $"<ul style=\"margin:0 0 20px;padding-left:18px;\">{reasonItems}</ul>" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 14px;\"><tr>" +
                "<td style=\"background:#eff6ff;border:1px solid #bfdbfe;border-radius:6px;padding:12px 16px;\">" +
                "<span style=\"color:#1e40af;font-size:13px;line-height:1.6;\">👉 <strong>Để gỡ khóa:</strong> đăng nhập trang quản trị, tìm người dùng theo email ở trên rồi <strong>xóa bớt 1 bản ghi từ chối</strong> để số lần còn dưới 2.</span>" +
                "</td></tr></table>" +
                $"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:2px 0;\"><tr><td style=\"background:#76b900;border-radius:6px;\"><a href=\"{adminLink}\" style=\"display:inline-block;padding:12px 26px;color:#ffffff;font-size:14px;font-weight:bold;text-decoration:none;\">Mở trang quản trị</a></td></tr></table>" +
                "</td></tr>" +
                "<tr><td style=\"background:#f9fafb;padding:14px 32px;border-top:1px solid #e5e7eb;\">" +
                "<p style=\"margin:0;color:#9ca3af;font-size:11px;line-height:1.6;\">Email tự động từ hệ thống SEAL · SU26-SWP-SE1907</p>" +
                "</td></tr></table></td></tr></table></div>";
        }

        private static string BuildUserEmail(User user, string supportEmail)
        {
            var safeName = WebUtility.HtmlEncode(user.FullName);
            var safeSupport = WebUtility.HtmlEncode(supportEmail);
            return
                "<div style=\"background:#f4f5f7;padding:24px 12px;font-family:Arial,Helvetica,sans-serif;\">" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\">" +
                "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background:#ffffff;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;\">" +
                "<tr><td style=\"background:#111418;padding:18px 32px;\">" +
                "<span style=\"color:#76b900;font-size:22px;font-weight:bold;letter-spacing:1px;\">SEAL</span>" +
                "<span style=\"color:#c9cdd2;font-size:12px;\"> &nbsp;·&nbsp; Yêu cầu gỡ khóa tài khoản</span>" +
                "</td></tr>" +
                "<tr><td style=\"padding:28px 32px 8px;\">" +
                "<h2 style=\"margin:0 0 14px;color:#111827;font-size:18px;\">Đã nhận yêu cầu gỡ khóa của bạn</h2>" +
                $"<p style=\"margin:0 0 14px;color:#374151;font-size:14px;line-height:1.6;\">Chào <strong>{safeName}</strong>,</p>" +
                "<p style=\"margin:0 0 14px;color:#374151;font-size:14px;line-height:1.6;\">Chúng tôi đã nhận được yêu cầu gỡ khóa tài khoản của bạn. Ban tổ chức sẽ xem xét lịch sử hồ sơ và phản hồi qua email trong thời gian sớm nhất.</p>" +
                $"<p style=\"margin:0 0 4px;color:#6b7280;font-size:13px;line-height:1.6;\">Nếu cần hỗ trợ gấp, vui lòng liên hệ: <a href=\"mailto:{safeSupport}\" style=\"color:#2563eb;\">{safeSupport}</a></p>" +
                "</td></tr>" +
                "<tr><td style=\"background:#f9fafb;padding:14px 32px;border-top:1px solid #e5e7eb;\">" +
                "<p style=\"margin:0;color:#9ca3af;font-size:11px;line-height:1.6;\">Email tự động từ hệ thống SEAL — vui lòng không trả lời email này.<br>© 2026 SEAL · SU26-SWP-SE1907</p>" +
                "</td></tr></table></td></tr></table></div>";
        }
    }
}

