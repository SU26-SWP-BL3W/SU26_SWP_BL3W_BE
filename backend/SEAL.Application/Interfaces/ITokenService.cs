using System.Collections.Generic;

namespace SEAL_Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(string userId, ICollection<string> roles);
        string GenerateRefreshToken();
    }
}
