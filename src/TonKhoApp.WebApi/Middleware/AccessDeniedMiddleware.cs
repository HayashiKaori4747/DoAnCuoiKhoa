using System.Text.Encodings.Web;
using System.Text.Json;
using TonKhoApp.Application.Common;

namespace TonKhoApp.WebApi.Middleware
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class PermissionNameAttribute : Attribute
    {
        public string Name { get; }
        public PermissionNameAttribute(string name) => Name = name;
    }

    public class AccessDeniedMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public AccessDeniedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                return;
            }

            if (context.Response.HasStarted || context.Response.ContentLength > 0)
            {
                return;
            }

            var status = context.Response.StatusCode;
            if (status != StatusCodes.Status401Unauthorized &&
                status != StatusCodes.Status403Forbidden)
            {
                return;
            }

            var message = status == StatusCodes.Status401Unauthorized
                ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                : BuildForbiddenMessage(context);

            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse.Fail(message), JsonOptions));
        }

        private static string BuildForbiddenMessage(HttpContext context)
        {
            var name = context.GetEndpoint()?
                .Metadata.GetMetadata<PermissionNameAttribute>()?.Name;

            var what = string.IsNullOrWhiteSpace(name)
                ? "thực hiện thao tác này"
                : name;

            return $"Bạn không có quyền {what}. " +
                   "Vai trò của tài khoản này không được phép. " +
                   "Liên hệ quản trị viên nếu bạn cần quyền đó.";
        }
    }

    public static class AccessDeniedMiddlewareExtensions
    {
        public static IApplicationBuilder UseAccessDeniedJson(this IApplicationBuilder app)
            => app.UseMiddleware<AccessDeniedMiddleware>();
    }
}
