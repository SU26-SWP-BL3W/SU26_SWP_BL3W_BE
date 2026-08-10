using Microsoft.AspNetCore.Http;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Helpers
{
    public class Authentication
    {
        public static Task HandleForbiddenRequest(HttpContext context)
        {
            throw new BaseException.ForbiddenException("Bạn không có quyền truy cập tính năng này!");
        }

        public static Task HandleUnAuthorized(HttpContext context)
        {
            throw new BaseException.UnauthorizedException("vui lòng đăng nhập");
        }

        public static Task HandleForbiddenRequest(IHttpContextAccessor context)
        {
            throw new BaseException.ForbiddenException("Bạn không có quyền truy cập tính năng này!");
        }
    }
}
