using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Infrastructure.Services;
using System.Threading.Tasks;

namespace SEAL_Backend.Filters
{
    public class AdminOrCoordinatorAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminOrCoordinatorAuthorizationFilter(IUnitOfWork unitOfWork)
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

            // 1. Kiểm tra Admin
            var user = await _unitOfWork.GetRepository<User>().GetQueryable().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && user.IsAdmin)
            {
                return; // Admin được phép
            }

            // 2. Kiểm tra xem user có phải là Event Coordinator của BẤT KỲ sự kiện nào không
            var now = System.DateTime.UtcNow;
            var isCoordinator = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                .AsNoTracking()
                .AnyAsync(er => er.UserId == userId 
                             && er.RoleName == EventRoleType.EventCoordinator
                             && (er.ExpiredAt == null || er.ExpiredAt > now));

            if (isCoordinator)
            {
                return; // EC được phép
            }

            // Nếu không phải Admin cũng không phải EC
            context.Result = new ObjectResult(new { message = "Chỉ Admin hệ thống hoặc Điều phối viên sự kiện (Event Coordinator) mới có quyền thực hiện thao tác này." })
            {
                StatusCode = 403
            };
        }
    }
}
