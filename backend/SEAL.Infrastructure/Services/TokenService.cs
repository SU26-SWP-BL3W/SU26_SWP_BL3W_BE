using Microsoft.Extensions.Options;
using SEAL_Application.Interfaces;
using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        public TokenService(IOptions<JwtSettings> jwtSettingsOptions)
        {
            _jwtSettings = jwtSettingsOptions.Value;
        }

        public string GenerateAccessToken(string userId, ICollection<string> roles)
        {
            return Token.GenerateAccessToken(userId, roles, _jwtSettings);
        }

        public string GenerateRefreshToken()
        {
            return Token.GenerateRefreshToken(_jwtSettings);
        }
    }
}
