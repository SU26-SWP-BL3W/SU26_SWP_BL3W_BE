using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Infrastructure.Services;
using System.Threading.Tasks;

namespace SEAL_Backend.Filters
{
    public class AdminAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminAuthorizationFilter(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userId = Token.GetUserIdFromHttpContext(context.HttpContext);
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            if (user == null || !user.IsAdmin)
            {
                context.Result = new ObjectResult(new { message = "Chỉ Admin hệ thống mới có quyền thực hiện thao tác này." })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
