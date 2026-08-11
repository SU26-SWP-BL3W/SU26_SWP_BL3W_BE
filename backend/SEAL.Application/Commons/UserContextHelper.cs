using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SEAL_Application.Commons
{
    public static class UserContextHelper
    {
        public static string? GetCurrentUserId(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst("id")?.Value
                         ?? httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId;
        }
    }
}
