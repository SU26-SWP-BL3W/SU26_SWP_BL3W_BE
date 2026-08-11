using Microsoft.AspNetCore.Mvc;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Backend.Filters
{
    public class EventRoleAuthorizeAttribute : TypeFilterAttribute
    {
        public EventRoleAuthorizeAttribute(params EventRoleType[] allowedRoles) 
            : base(typeof(EventRoleAuthorizationFilter))
        {
            Arguments = new object[] { allowedRoles };
        }
    }
}
