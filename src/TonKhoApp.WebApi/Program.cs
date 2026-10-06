using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using TonKhoApp.Application;
using TonKhoApp.Application.Common;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Application.Services;
using TonKhoApp.Infrastructure;
using TonKhoApp.WebApi.Middleware;
using TonKhoApp.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "TONKHO_");

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value != null && kv.Value.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "Giá trị không hợp lệ"
                            : e.ErrorMessage)
                        .ToArray());

            var firstError = errors.Values.SelectMany(m => m).FirstOrDefault()
                             ?? "Dữ liệu gửi lên không hợp lệ";

            return new BadRequestObjectResult(ApiResponse.Fail(firstError, errors));
        };
    });

builder.Services.AddApplication();                          
builder.Services.AddInfrastructure(builder.Configuration);  

builder.Services.AddDatabaseKeepAlive(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secretKeyValue = builder.Configuration["JWTSECRETKEY"] ?? jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKeyValue) || Encoding.UTF8.GetByteCount(secretKeyValue) < 32)
{
    throw new InvalidOperationException(
        "Chưa cấu hình JwtSettings:SecretKey (tối thiểu 32 ký tự). Đặt biến môi trường " +
        "TONKHO_JWTSECRETKEY hoặc chạy: dotnet user-secrets set \"JwtSettings:SecretKey\" \"<khoá bí mật>\"");
}
var secretKey = Encoding.UTF8.GetBytes(secretKeyValue);

if (string.IsNullOrWhiteSpace(jwtSettings["Issuer"]) ||
    string.IsNullOrWhiteSpace(jwtSettings["Audience"]))
{
    throw new InvalidOperationException(
        "Chưa cấu hình JwtSettings:Issuer và JwtSettings:Audience trong appsettings.json.");
}
if (!int.TryParse(jwtSettings["ExpirationMinutes"], out var jwtExpirationMinutes) ||
    jwtExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "JwtSettings:ExpirationMinutes phải là một số nguyên dương (số phút token còn hiệu lực).");
}

var apiJsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,              // Kiểm tra Issuer
        ValidateAudience = true,            // Kiểm tra Audience
        ValidateLifetime = true,            // Kiểm tra hạn token
        ValidateIssuerSigningKey = true,    // Kiểm tra chữ ký
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var idText = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idText, out var userId))
            {
                context.Fail(new SecurityTokenException(
                    "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại."));
                return;
            }

            var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

            if (user == null || !user.IsActive)
            {
                context.Fail(new SecurityTokenException(
                    "Tài khoản đã bị khoá hoặc không còn tồn tại. Vui lòng liên hệ quản trị viên."));
                return;
            }

            var rolesInToken = context.Principal?
                .FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal)
                ?? new HashSet<string>(StringComparer.Ordinal);

            var rolesInDb = (await users.GetRoleCodesAsync(userId, context.HttpContext.RequestAborted))
                .ToHashSet(StringComparer.Ordinal);

            if (!rolesInToken.SetEquals(rolesInDb))
            {
                context.Fail(new SecurityTokenException(
                    "Quyền của tài khoản vừa được thay đổi. Vui lòng đăng nhập lại."));
            }
        },

        OnChallenge = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json; charset=utf-8";

                var message = context.AuthenticateFailure is SecurityTokenException tokenError
                    ? tokenError.Message
                    : "Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.";

                return context.Response.WriteAsync(
                    JsonSerializer.Serialize(ApiResponse.Fail(message), apiJsonOptions));
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();

    var knownProxies = builder.Configuration.GetSection("Security:KnownProxies").Get<string[]>();
    foreach (var proxy in knownProxies ?? Array.Empty<string>())
    {
        if (IPAddress.TryParse(proxy, out var parsed))
        {
            options.KnownProxies.Add(parsed);
        }
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Bạn đã thử đăng nhập quá nhiều lần. " +
            "Vui lòng đợi 1 phút rồi thử lại.\"}", cancellationToken);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "assets")),
    RequestPath = "/assets"
});

// Routing
app.UseRouting();

app.UseRateLimiter();

app.UseAccessDeniedJson();

app.UseAuthentication();

app.UsePageAuth();
app.UseAuthorization();

app.MapControllers();

var webRoot = app.Environment.WebRootPath;

app.MapGet("/", () => Results.Redirect("/login"));
app.MapGet("/login", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "login.html"));
});
app.MapGet("/orders-list", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "orders-list.html"));
});
app.MapGet("/order-detail", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "order-detail.html"));
});
app.MapGet("/products-list", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "products-list.html"));
});
app.MapGet("/product-detail", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "product-detail.html"));
});
app.MapGet("/vendors-list", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "vendors-list.html"));
});
app.MapGet("/vendor-detail", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "vendor-detail.html"));
});
app.MapGet("/stock", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "stock.html"));
});
app.MapGet("/dashboard", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "dashboard.html"));
});
app.MapGet("/users-list", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "users-list.html"));
});
app.MapGet("/roles-list", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "roles-list.html"));
});
app.MapGet("/audit-logs", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(webRoot, "pages", "audit-logs.html"));
});
app.MapGet("/logout", async context =>
{
    var authService = context.RequestServices.GetRequiredService<IAuthService>();
    await authService.LogoutAsync(context.Request.Cookies["refresh_token"], context.RequestAborted);

    context.Response.Cookies.Delete("jwt_token");
    context.Response.Cookies.Delete("user_info");

    context.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });

    context.Response.Redirect("/login");
});

await DatabaseWakeup.WarmUpAsync(app);

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH")))
{
    Console.WriteLine("Chạy dưới IIS - để IIS quyết định địa chỉ lắng nghe.");
    app.Run();
    return;
}

var listenUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(listenUrl))
{
    var port = Environment.GetEnvironmentVariable("PORT");
    listenUrl = string.IsNullOrWhiteSpace(port)
        ? "http://localhost:3000"      
        : $"http://0.0.0.0:{port}";    
}

Console.WriteLine($"Server đang chạy tại: {listenUrl}");
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("Swagger UI: /swagger");
}

app.Run(listenUrl);
