using Microsoft.AspNetCore.Http;
using SEAL_Application.Interfaces;
using System.Security.Claims;

namespace SEAL_Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var userId = httpContext?.User?.FindFirst("id")?.Value
                             ?? httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return userId;
            }
        }
    }
}
