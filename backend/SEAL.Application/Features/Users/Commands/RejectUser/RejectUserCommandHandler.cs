using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.RejectUser
{
    public class RejectUserCommandHandler : IRequestHandler<RejectUserCommand, Result<UserModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RejectUserCommandHandler> _logger;

        public RejectUserCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<RejectUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Result<UserModel>> Handle(RejectUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra người gọi (caller)
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            // 2. Tìm người dùng bị từ chối
            var userToReject = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Id);
            if (userToReject == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{request.Id}' không tồn tại.");
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                return BaseException.BadRequestResponse("Lý do từ chối không được để trống.");
            }

            // 3. Tìm các vai trò trong sự kiện của người dùng bị từ chối
            var userRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.UserId == userToReject.Id && er.TeamId != null)
                .ToListAsync(cancellationToken);

            var eventIds = userRoles.Select(er => er.EventId).Distinct().ToList();

            // 4. Kiểm tra quyền từ chối
            if (!isAdmin)
            {
                bool isCoordinatorOfAnyEvent = false;
                foreach (var eventId in eventIds)
                {
                    var isCoord = await _eventRoleChecker.HasRoleAsync(
                        currentUserId, eventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
                    if (isCoord)
                    {
                        isCoordinatorOfAnyEvent = true;
                        break;
                    }
                }

                if (!isCoordinatorOfAnyEvent)
                {
                    return new BaseException.ForbiddenException("Chỉ Admin hệ thống hoặc Event Coordinator liên quan mới được phép từ chối hồ sơ.");
                }
            }

            // 5. Từ chối người dùng và đặt IsApproved = false
            userToReject.IsApproved = false;
            userToReject.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<User>().UpdateAsync(userToReject);

            // 6. Lưu lịch sử từ chối vào UserRejection
            var userRejection = new UserRejection
            {
                UserId = userToReject.Id,
                RejectedBy = currentUserId,
                CreatedBy = currentUserId,
                Reason = request.Reason,
                IsActive = true
            };
            await _unitOfWork.GetRepository<UserRejection>().AddAsync(userRejection);

            // 7. Cập nhật các đội liên quan về trạng thái Forming và không active
            var teamIds = userRoles.Select(er => er.TeamId).Where(id => id != null).Distinct().ToList();
            foreach (var teamId in teamIds)
            {
                var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(teamId!);
                if (team != null && (team.Status == TeamStatus.Registered || team.IsActive))
                {
                    team.Status = TeamStatus.Forming;
                    team.IsActive = false;
                    team.LastUpdatedTime = CoreHelper.SystemTimeNow;
                    await _unitOfWork.GetRepository<Team>().UpdateAsync(team);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. Gửi email thông báo bị từ chối kèm lý do + link sửa hồ sơ.
            //    Bọc try/catch để lỗi gửi mail không làm hỏng thao tác từ chối (đã lưu DB thành công).
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://swp391-frontend.vercel.app";
                var editLink = $"{frontendUrl}/profile";
                // Escape HTML để lý do/tên có ký tự đặc biệt (< > &) không làm vỡ email.
                var safeName = System.Net.WebUtility.HtmlEncode(userToReject.FullName);
                var safeReason = System.Net.WebUtility.HtmlEncode(request.Reason);
                var emailBody =
                    "<div style=\"background:#f4f5f7;padding:24px 12px;font-family:Arial,Helvetica,sans-serif;\">" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\">" +
                    "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background:#ffffff;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;\">" +
                    "<tr><td style=\"background:#111418;padding:18px 32px;\">" +
                    "<span style=\"color:#76b900;font-size:22px;font-weight:bold;letter-spacing:1px;\">SEAL</span>" +
                    "<span style=\"color:#c9cdd2;font-size:12px;\"> &nbsp;·&nbsp; Hệ thống quản lý &amp; chấm thi sự kiện</span>" +
                    "</td></tr>" +
                    "<tr><td style=\"padding:32px 32px 8px;\">" +
                    "<h2 style=\"margin:0 0 14px;color:#111827;font-size:19px;\">Hồ sơ của bạn đã bị từ chối</h2>" +
                    $"<p style=\"margin:0 0 14px;color:#374151;font-size:14px;line-height:1.6;\">Chào <strong>{safeName}</strong>,</p>" +
                    "<p style=\"margin:0 0 18px;color:#374151;font-size:14px;line-height:1.6;\">Rất tiếc, hồ sơ đăng ký của bạn trên hệ thống <strong>SEAL</strong> chưa được duyệt. Vui lòng xem lý do bên dưới và cập nhật lại hồ sơ.</p>" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 24px;\"><tr>" +
                    "<td style=\"background:#fef2f2;border-left:4px solid #ef4444;border-radius:4px;padding:14px 16px;\">" +
                    "<div style=\"color:#991b1b;font-size:11px;font-weight:bold;letter-spacing:.5px;margin-bottom:5px;\">LÝ DO TỪ CHỐI</div>" +
                    $"<div style=\"color:#374151;font-size:14px;line-height:1.5;\">{safeReason}</div>" +
                    "</td></tr></table>" +
                    "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 14px;\"><tr>" +
                    $"<td style=\"background:#76b900;border-radius:6px;\"><a href=\"{editLink}\" style=\"display:inline-block;padding:13px 30px;color:#ffffff;font-size:14px;font-weight:bold;text-decoration:none;\">Cập nhật hồ sơ ngay →</a></td>" +
                    "</tr></table>" +
                    $"<p style=\"margin:0 0 4px;color:#6b7280;font-size:12px;line-height:1.5;\">Hoặc mở liên kết: <a href=\"{editLink}\" style=\"color:#2563eb;\">{editLink}</a></p>" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:20px 0 8px;\"><tr>" +
                    "<td style=\"background:#fffbeb;border:1px solid #fde68a;border-radius:6px;padding:12px 16px;\">" +
                    "<span style=\"color:#92400e;font-size:13px;line-height:1.5;\">⚠️ <strong>Lưu ý:</strong> Nếu hồ sơ bị từ chối <strong>quá 2 lần</strong>, bạn sẽ không thể cập nhật và cần liên hệ ban tổ chức.</span>" +
                    "</td></tr></table>" +
                    "</td></tr>" +
                    "<tr><td style=\"background:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;\">" +
                    "<p style=\"margin:0;color:#9ca3af;font-size:11px;line-height:1.6;\">Đây là email tự động từ hệ thống SEAL — vui lòng không trả lời email này.<br>© 2026 SEAL · SU26-SWP-SE1907</p>" +
                    "</td></tr></table></td></tr></table></div>";
                await _emailService.SendEmailAsync(userToReject.Email, "[SEAL] Hồ sơ của bạn đã bị từ chối", emailBody);
            }
            catch (Exception ex)
            {
                // Bỏ qua lỗi gửi email — thao tác từ chối đã được lưu thành công.
                _logger.LogWarning(ex, "Gửi email thông báo từ chối hồ sơ thất bại cho {Email}", userToReject.Email);
            }

            return new UserModel
            {
                Id = userToReject.Id,
                SchoolId = userToReject.SchoolId ?? string.Empty,
                StudentCode = userToReject.StudentCode,
                Email = userToReject.Email,
                FullName = userToReject.FullName,
                IsStudent = userToReject.IsStudent,
                IsAdmin = userToReject.IsAdmin,
                IsApproved = userToReject.IsApproved,
                IsFpt = userToReject.IsFpt,
                PhotoStudentCardUrl = userToReject.PhotoStudentCardUrl
            };
        }
    }
}


