using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Base;
using SEAL_Domain.Store;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEAL_Backend.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            StatusCodeHelper statusCodeHelper;
            object? errorData = null;
            string? message = exception.Message;

            switch (exception)
            {
                case BaseException.UnauthorizedException unauthorizedEx:
                    statusCodeHelper = StatusCodeHelper.Unauthorized;
                    message = unauthorizedEx.Message;
                    break;

                case BaseException.ForbiddenException forbiddenEx:
                    statusCodeHelper = StatusCodeHelper.Forbidden;
                    message = forbiddenEx.Message;
                    break;

                case BaseException.ErrorException errorEx:
                    statusCodeHelper = Enum.IsDefined(typeof(StatusCodeHelper), errorEx.StatusCode)
                        ? (StatusCodeHelper)errorEx.StatusCode
                        : StatusCodeHelper.ServerError;
                    errorData = errorEx.ErrorDetail.ErrorMessage;
                    message = errorEx.Message;
                    break;

                default:
                    statusCodeHelper = StatusCodeHelper.ServerError;
                    message = "An unexpected error occurred.";
                    break;
            }

            context.Response.StatusCode = (int)statusCodeHelper;

            var response = new BaseResponse<object>(
                statusCode: statusCodeHelper,
                code: string.Empty,
                data: errorData,
                message: message
            );

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsJsonAsync(response, options);
        }
    }
}
