using Microsoft.AspNetCore.Mvc;

namespace SEAL_Backend.Filters
{
    public class AdminOrCoordinatorAuthorizeAttribute : TypeFilterAttribute
    {
        public AdminOrCoordinatorAuthorizeAttribute() : base(typeof(AdminOrCoordinatorAuthorizationFilter))
        {
        }
    }
}
