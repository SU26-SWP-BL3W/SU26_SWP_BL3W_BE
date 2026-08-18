using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<ChangePasswordCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy thông tin người dùng.");
            }

            var hashedOldPassword = FixedSaltPasswordHasher.HashPassword(request.Model.OldPassword);
            if (user.PasswordHash != hashedOldPassword)
            {
                return BaseException.BadRequestInvaildInputResponse("Mật khẩu cũ không chính xác.");
            }

            user.PasswordHash = FixedSaltPasswordHasher.HashPassword(request.Model.NewPassword);
            user.MustChangePassword = false;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Cảnh báo bảo mật: mật khẩu vừa đổi — giống hệt mail của luồng reset-password qua link,
            // để nhất quán dù đổi bằng cách nào (đổi tay từ hồ sơ, hay bắt buộc sau khi kích hoạt tài khoản tạm).
            try
            {
                var body = EmailTemplate.Render(
                    heading: "Mật khẩu vừa được thay đổi",
                    greetingName: user.FullName,
                    introHtml: $"Mật khẩu của tài khoản <b>{user.Email}</b> vừa được thay đổi.",
                    calloutLabel: "Cảnh báo bảo mật",
                    calloutHtml: "Nếu <b>không phải bạn</b> thực hiện, hãy liên hệ ban tổ chức ngay để bảo vệ tài khoản.",
                    calloutKind: EmailTemplate.Callout.Warning,
                    showLoginHint: false);
                await _emailService.SendEmailAsync(user.Email, "[SEAL] Mật khẩu của bạn vừa được thay đổi", body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gửi email cảnh báo đổi mật khẩu thất bại cho {Email}", user.Email);
            }

            return true;
        }
    }
}

