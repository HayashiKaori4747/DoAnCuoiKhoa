namespace TonKhoApp.WebApi.Middleware
{
    public class PageAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public PageAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/login", "/logout",
            "/api/auth/login", "/api/auth/logout"
        };

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (PublicPaths.Contains(path))
            {
                await _next(context);
                return;
            }

            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            context.Response.Cookies.Delete("jwt_token");
            context.Response.Cookies.Delete("user_info");

            var returnUrl = path + context.Request.QueryString;
            context.Response.Redirect("/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
        }
    }

    public static class PageAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UsePageAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PageAuthMiddleware>();
        }
    }
}
