using SEAL_Domain.Store;
using static SEAL_Domain.Store.Constants;

namespace SEAL_Domain.Base
{
    public class BaseException : Exception
    {
        public class CoreException : Exception
        {
            public CoreException(string code, string message, int statusCode = (int)StatusCodeHelper.ServerError)
                : base(message)
            {
                Code = code;
                StatusCode = statusCode;
            }


            public string Code { get; }

            public int StatusCode { get; set; }

            public Dictionary<string, object>? AdditionalData { get; set; }
        }

        public class BadRequestException : ErrorException
        {
            public BadRequestException(string errorCode, string message)
                : base(400, errorCode, message)
            {
            }
            public BadRequestException(ICollection<KeyValuePair<string, ICollection<string>>> errors)
                : base(400, new ErrorDetail
                {
                    ErrorCode = "bad_request",
                    ErrorMessage = errors
                })
            {
            }
        }

        public class ErrorException : Exception
        {
            public int StatusCode { get; }

            public ErrorDetail ErrorDetail { get; }

            public ErrorException(int statusCode, string errorCode, string message) : base(message)
            {
                StatusCode = statusCode;
                ErrorDetail = new ErrorDetail
                {
                    ErrorCode = errorCode,
                    ErrorMessage = message
                };
            }

            public ErrorException(int statusCode, ErrorDetail errorDetail)
            {
                StatusCode = statusCode;
                ErrorDetail = errorDetail;
            }

        }
        public class UnauthorizedException : Exception
        {
            public UnauthorizedException(string message) : base(message) { }
        }

        public class ForbiddenException : Exception
        {
            public ForbiddenException(string message) : base(message) { }
        }

        public class ErrorDetail
        {
            public string? ErrorCode { get; set; }

            public object? ErrorMessage { get; set; }
        }

        public static ErrorException BadRequestInvaildInputResponse(string mess)
        {
            return new ErrorException((int)StatusCodeHelper.BadRequest, ResponseCodeConstants.INVALID_INPUT, mess);
        }

        public static ErrorException BadRequestNotFoundResponse(string mess)
        {
            return new ErrorException((int)StatusCodeHelper.BadRequest, ResponseCodeConstants.NOT_FOUND, mess);
        }

        public static ErrorException BadRequestResponse(string mess)
        {
            return new ErrorException((int)StatusCodeHelper.BadRequest, ResponseCodeConstants.BADREQUEST, mess);
        }

        public static ErrorException BadRequestNotFoundResponse()
        {
            return new ErrorException((int)StatusCodeHelper.BadRequest, ResponseCodeConstants.NOT_FOUND, ResponseCodeConstants.NOT_FOUND);
        }

        public static ErrorException BadRequestDupplicationResponse(string mess)
        {
            return new ErrorException((int)StatusCodeHelper.BadRequest, ResponseCodeConstants.DUPLICATE, mess);
        }

        public static ErrorException UnauthorizedUnAuthorizedResponse()
        {
            return new ErrorException((int)StatusCodeHelper.Unauthorized, ResponseCodeConstants.UNAUTHORIZED, ResponseCodeConstants.UNAUTHORIZED);
        }

        public static ErrorException ForbiddenResponse(string message)
        {
            return new ErrorException((int)StatusCodeHelper.Forbidden, ResponseCodeConstants.FORBIDDEN, message);
        }
    }
}
