using Microsoft.AspNetCore.Mvc;
using SEAL_Domain.Store;
using static SEAL_Domain.Store.Constants;
using SEAL_Domain.Base;

namespace SEAL_Backend.Helpers
{
    public class CustomControllerBase : ControllerBase
    {
        [NonAction]
        public IActionResult OkResponse<T>(T? data)
        {
            BaseResponse<T> rs = new(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: data,
                message: MessagesConstants.SUCCESS
            );
            return base.Ok(rs);
        }

        [NonAction]
        public IActionResult OkResponse(Result? result)
        {
            if (result == null)
            {
                BaseResponse<object> errRs = new(
                    statusCode: StatusCodeHelper.ServerError,
                    code: string.Empty,
                    data: null,
                    message: "Kết quả trả về bị null"
                );
                return base.StatusCode((int)StatusCodeHelper.ServerError, errRs);
            }

            if (result.IsFailure)
            {
                BaseResponse<object> errRs = new(
                    statusCode: Enum.IsDefined(typeof(StatusCodeHelper), result.StatusCode)
                        ? (StatusCodeHelper)result.StatusCode
                        : StatusCodeHelper.ServerError,
                    code: result.Error?.ErrorCode ?? string.Empty,
                    data: result.Error?.ErrorMessage,
                    message: result.Error?.ErrorMessage?.ToString() ?? "Đã xảy ra lỗi"
                );
                return base.StatusCode(result.StatusCode, errRs);
            }

            BaseResponse<object> rs = new(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: null,
                message: MessagesConstants.SUCCESS
            );
            return base.Ok(rs);
        }

        [NonAction]
        public IActionResult OkResponse<T>(Result<T>? result)
        {
            if (result == null)
            {
                BaseResponse<object> errRs = new(
                    statusCode: StatusCodeHelper.ServerError,
                    code: string.Empty,
                    data: null,
                    message: "Kết quả trả về bị null"
                );
                return base.StatusCode((int)StatusCodeHelper.ServerError, errRs);
            }

            if (result.IsFailure)
            {
                return OkResponse((Result)result);
            }

            BaseResponse<T> rs = new(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result.Value,
                message: MessagesConstants.SUCCESS
            );
            return base.Ok(rs);
        }
    }
}
