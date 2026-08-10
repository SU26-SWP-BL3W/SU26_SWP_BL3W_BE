using Microsoft.AspNetCore.Mvc;

namespace SEAL_Backend.Filters
{
    public class AdminAuthorizeAttribute : TypeFilterAttribute
    {
        public AdminAuthorizeAttribute() : base(typeof(AdminAuthorizationFilter))
        {
        }
    }
}
