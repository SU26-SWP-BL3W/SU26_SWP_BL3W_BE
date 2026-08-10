using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using SEAL_Domain.Base;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static SEAL_Domain.Base.BaseException;

namespace SEAL_Infrastructure.Services
{
    public static class Token
    {
        public static string GenerateAccessToken(string userId, ICollection<string> roles, JwtSettings jwtSettings)
        {
            List<Claim> claims = new()
            {
                new Claim("id", userId),
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }
            string keyString = jwtSettings.SecretKey ?? string.Empty;
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(keyString));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha512Signature);
            
            var token = new JwtSecurityToken(
                claims: claims,
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                expires: DateTime.Now.AddMinutes(jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateRefreshToken(JwtSettings jwtSettings)
        {
            // Refresh token = chuỗi ngẫu nhiên đối xứng (opaque), được lưu & đối chiếu ở server
            // qua User.RefreshToken + RefreshTokenExpiryTime. KHÔNG dùng JWT chỉ-có-exp: 2 người
            // đăng nhập trong cùng 1 giây sẽ nhận token TRÙNG nhau -> lookup SingleOrDefault khớp
            // nhiều dòng -> ném lỗi 500 (và là lỗ hổng account-takeover tiềm ẩn).
            // 64 byte ngẫu nhiên đảm bảo mỗi refresh token là duy nhất.
            byte[] randomNumber = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }

        public static string RefreshAccessToken(string userId, string refreshToken, JwtSettings jwtSettings)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = key,
                ValidateLifetime = true
            };

            SecurityToken validatedToken;
            ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(refreshToken, validationParameters, out validatedToken);

            if (validatedToken.ValidTo < DateTime.UtcNow)
            {
                throw new SecurityTokenExpiredException("Refresh Token has expired.");
            }

            var newAccessToken = GenerateAccessToken(userId, claimsPrincipal.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList(), jwtSettings);
            return newAccessToken;
        }

        public static bool IsTokenValid(string bearerToken, JwtSettings jwtSettings)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            try
            {
                TokenValidationParameters tokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey!)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                ClaimsPrincipal principal = tokenHandler.ValidateToken(bearerToken, tokenValidationParameters, out var validatedToken);

                JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
                return jwtToken.ValidTo > DateTime.UtcNow;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetUserIdFromHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            try
            {
                if (httpContextAccessor.HttpContext == null || !httpContextAccessor.HttpContext.Request.Headers.ContainsKey("Authorization"))
                {
                    throw new UnauthorizedException("Need Authorization");
                }

                string? authorizationHeader = httpContextAccessor.HttpContext.Request.Headers["Authorization"];

                if (string.IsNullOrWhiteSpace(authorizationHeader))
                {
                    throw new UnauthorizedException("Authorization header is empty");
                }

                string jwtToken;
                if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    jwtToken = authorizationHeader["Bearer ".Length..].Trim();
                }
                else
                {
                    jwtToken = authorizationHeader.Trim();
                }

                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(jwtToken))
                {
                    throw new UnauthorizedException("Invalid token format");
                }

                var token = tokenHandler.ReadJwtToken(jwtToken);

                var idClaim = token.Claims.FirstOrDefault(claim => claim.Type == "id")
                              ?? token.Claims.FirstOrDefault(claim => claim.Type == "nameid")
                              ?? token.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);

                return idClaim?.Value ?? throw new UnauthorizedException("Cannot get userId from token");
            }
            catch (UnauthorizedException)
            {
                throw;
            }
        }

        public static string GetUserIdFromHttpContext(HttpContext httpContext)
        {
            try
            {
                if (!httpContext.Request.Headers.ContainsKey("Authorization"))
                {
                    throw new UnauthorizedException("Need Authorization");
                }

                string? authorizationHeader = httpContext.Request.Headers["Authorization"];

                if (string.IsNullOrWhiteSpace(authorizationHeader))
                {
                    throw new UnauthorizedException("Authorization header is empty");
                }

                string jwtToken;
                if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    jwtToken = authorizationHeader["Bearer ".Length..].Trim();
                }
                else
                {
                    jwtToken = authorizationHeader.Trim();
                }
                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(jwtToken))
                {
                    throw new UnauthorizedException("Invalid token format");
                }

                var token = tokenHandler.ReadJwtToken(jwtToken);

                var idClaim = token.Claims.FirstOrDefault(claim => claim.Type == "id")
                              ?? token.Claims.FirstOrDefault(claim => claim.Type == "nameid")
                              ?? token.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);

                return idClaim?.Value ?? throw new UnauthorizedException("Cannot get userId from token");
            }
            catch (UnauthorizedException)
            {
                throw;
            }
        }
    }
}
