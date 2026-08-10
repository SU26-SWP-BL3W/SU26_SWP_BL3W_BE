using SEAL_Domain.Store;
using SEAL_Domain.Ultis;

namespace SEAL_Domain.Base
{
    public class BaseResponse<T>
    {
        public T? Data { get; set; }
        public string? Message { get; set; }
        public StatusCodeHelper StatusCode { get; set; }
        public bool Success => StatusCode == StatusCodeHelper.OK;
        //public string? Code { get; set; }
        public BaseResponse(StatusCodeHelper statusCode, string code, T? data, string? message)
        {
            Data = data;
            Message = message;
            StatusCode = statusCode;
            //Code = code;
        }

        public BaseResponse(StatusCodeHelper statusCode, string code, T? data)
        {
            Data = data;
            StatusCode = statusCode;
            //Code = code;
        }

        public BaseResponse(StatusCodeHelper statusCode, string code, string? message)
        {
            Message = message;
            StatusCode = statusCode;
            //Code = code;
        }

        public static BaseResponse<T> OkResponse(T? data, string mess)
        {
            return new BaseResponse<T>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), data, mess);
        }

        public static BaseResponse<T> OkResponse(T? data)
        {
            return new BaseResponse<T>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), data);
        }

        public static BaseResponse<T> OkResponse(string? mess)
        {
            return new BaseResponse<T>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), mess);
        }

        public static BaseResponse<T> UnauthorizedResponse(string? message)
        {
            return new BaseResponse<T>(StatusCodeHelper.Unauthorized, StatusCodeHelper.Unauthorized.Name(), default(T), message);
        }
        public static BaseResponse<T> ForbiddenResponse(string? message)
        {
            return new BaseResponse<T>(StatusCodeHelper.Forbidden, StatusCodeHelper.Forbidden.Name(), default(T), message);
        }
        public static BaseResponse<T> BadRequestResponse(string? message)
        {
            return new BaseResponse<T>(StatusCodeHelper.BadRequest, StatusCodeHelper.BadRequest.Name(), default(T), message);
        }
        public static BaseResponse<T> NotFoundResponse(string? message)
        {
            return new BaseResponse<T>(StatusCodeHelper.NotFound, StatusCodeHelper.NotFound.Name(), default(T), message);
        }
        public static BaseResponse<T> InternalServerErrorResponse(string? message)
        {
            return new BaseResponse<T>(StatusCodeHelper.ServerError, StatusCodeHelper.ServerError.Name(), default(T), message);
        }


    }
    public static class ResponseHelper
    {
        public static BaseResponse<string> DeleteResponse()
        {
            return new BaseResponse<string>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), Constants.MessagesConstants.DELETE_SUCCESS);
        }

        public static BaseResponse<string> UpdateResponse()
        {
            return new BaseResponse<string>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), Constants.MessagesConstants.UPDATE_SUCCESS);
        }
        public static BaseResponse<string> CreateResponse()
        {
            return new BaseResponse<string>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), Constants.MessagesConstants.CREATE_SUCCESS);
        }

        public static BaseResponse<string> DeleteResponse(string? mess)
        {
            return new BaseResponse<string>(StatusCodeHelper.OK, StatusCodeHelper.OK.Name(), mess);
        }

        public static BaseResponse<T> UnauthorizedResponse<T>(string? message)
        {
            return new BaseResponse<T>(StatusCodeHelper.Unauthorized, StatusCodeHelper.Unauthorized.Name(), message);
        }
    }
}
