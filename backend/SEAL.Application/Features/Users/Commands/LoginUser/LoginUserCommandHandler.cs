using MediatR;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Users.Commands.LoginUser.Models;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SEAL_Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<LoginUserResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;

        public LoginUserCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService, Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<LoginUserResponseModel>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetRepository<User>().GetQueryable()
                .SingleOrDefaultAsync(u => u.Email.ToLower() == request.Model.Email.ToLower(), cancellationToken);

            if (user == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Email hoặc mật khẩu không chính xác.");
            }

            var hashedInputPassword = FixedSaltPasswordHasher.HashPassword(request.Model.Password);
            if (user.PasswordHash != hashedInputPassword)
            {
                return BaseException.BadRequestInvaildInputResponse("Email hoặc mật khẩu không chính xác.");
            }

            if (!user.IsAdmin && !user.IsEmailVerified && !user.IsTemporary)
            {
                return BaseException.BadRequestInvaildInputResponse("Tài khoản của bạn chưa được xác thực email.");
            }

            // Tài khoản tạm (giám khảo được mời) chỉ dùng được trong vòng đời sự kiện (check qua EventRole)
            if (user.IsTemporary)
            {
                var now = System.DateTime.UtcNow;
                var hasActiveRole = await _unitOfWork.GetRepository<EventRole>().Entities
                    .Include(er => er.Event)
                    .AnyAsync(er => er.UserId == user.Id && (er.ExpiredAt ?? er.Event.EndDate) > now, cancellationToken);
                if (!hasActiveRole)
                {
                    // Tài khoản tạm do MỜI VÀO ĐỘI tạo ra chưa có EventRole (chỉ có sau khi CHẤP NHẬN lời mời)
                    // -> cho phép đăng nhập khi còn lời mời đang chờ, nếu không sẽ kẹt vòng lặp:
                    // đăng nhập cần EventRole, EventRole cần chấp nhận lời mời, chấp nhận cần đăng nhập.
                    var hasPendingTeamInvite = await _unitOfWork.GetRepository<TeamInvitation>().Entities
                        .AnyAsync(inv => inv.InvitedUserId == user.Id
                                      && inv.Status == TeamInvitationStatus.PendingAccept
                                      && inv.ExpiresAt > now, cancellationToken);
                    // Tài khoản tạm do MỜI VAI TRÒ (EC/Giám khảo/Cố vấn) tạo ra chỉ có EventRoleInvitation,
                    // KHÔNG có TeamInvitation — cũng phải cho đăng nhập để vào chuông bấm Đồng ý, nếu không
                    // sẽ kẹt đúng vòng lặp trên (login cần EventRole, EventRole cần chấp nhận lời mời, chấp nhận cần login).
                    var hasPendingRoleInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().Entities
                        .AnyAsync(inv => inv.InvitedUserId == user.Id
                                      && inv.Status == EventRoleInvitationStatus.Pending
                                      && inv.ExpiresAt > now, cancellationToken);
                    if (!hasPendingTeamInvite && !hasPendingRoleInvite)
                    {
                        return BaseException.BadRequestInvaildInputResponse("Tài khoản tạm đã hết hạn theo vòng đời sự kiện.");
                    }
                }
            }

            var roles = new List<string>();
            if (user.IsAdmin) roles.Add("Admin");
            if (user.IsStudent) roles.Add("Student");

            var accessToken = _tokenService.GenerateAccessToken(user.Id, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = System.DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            _unitOfWork.GetRepository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

