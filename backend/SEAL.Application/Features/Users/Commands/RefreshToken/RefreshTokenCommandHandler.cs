using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Users.Commands.RefreshToken.Models;
using System.Threading;
using System.Threading.Tasks;
using SEAL_Application.Interfaces;
using SEAL_Domain.Base;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;

namespace SEAL_Application.Features.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;

        public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService, IOptions<JwtSettings> jwtSettings)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<RefreshTokenResponseModel>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetRepository<User>().GetQueryable()
                .SingleOrDefaultAsync(u => u.RefreshToken == request.Model.RefreshToken, cancellationToken);

            if (user == null)
            {
                return new BaseException.UnauthorizedException("Invalid Refresh Token");
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new BaseException.UnauthorizedException("Refresh Token has expired");
            }

            if (!user.IsAdmin && !user.IsEmailVerified && !user.IsTemporary)
            {
                return new BaseException.UnauthorizedException("Tài khoản của bạn chưa được xác thực email.");
            }

            var roles = new List<string>();
            if (user.IsAdmin) roles.Add("Admin");
            if (user.IsStudent) roles.Add("Student");

            var newAccessToken = _tokenService.GenerateAccessToken(user.Id, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            _unitOfWork.GetRepository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshTokenResponseModel
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
    }
}


