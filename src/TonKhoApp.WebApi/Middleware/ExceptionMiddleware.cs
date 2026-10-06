using System.Text.Encodings.Web;
using System.Text.Json;
using TonKhoApp.Application.Common;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.WebApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;   
        private readonly ILogger<ExceptionMiddleware> _logger; 

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            catch (NotFoundException ex)
            {
                await WriteResponseAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (ConcurrencyConflictException ex)
            {
                _logger.LogWarning(ex, "Xung đột dữ liệu đồng thời tại {Path}", context.Request.Path);
                await WriteResponseAsync(context, StatusCodes.Status409Conflict, ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Client huỷ request tại {Path}", context.Request.Path);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Unhandled exception tại {Path}", context.Request.Path);

                await WriteResponseAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Lỗi hệ thống, vui lòng thử lại sau");
            }
        }

        private async Task WriteResponseAsync(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "Không gửi được thân lỗi cho {Path}: response đã bắt đầu được gửi đi. " +
                    "Client sẽ thấy một response bị cắt ngang.", context.Request.Path);
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail(message);

            var json = JsonSerializer.Serialize(response, JsonOptions);

            await context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
