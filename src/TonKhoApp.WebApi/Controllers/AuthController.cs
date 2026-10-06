using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Services;
using TonKhoApp.WebApi.Middleware;

namespace TonKhoApp.WebApi.Controllers
{
    [ApiController] 
    [Route("api/auth")] 
    public class AuthApiController : ControllerBase
    {
        private const string AccessTokenCookie = "jwt_token";
        private const string RefreshTokenCookie = "refresh_token";
        private const string UserInfoCookie = "user_info";

        private const string RefreshCookiePath = "/api/auth";

        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public AuthApiController(
            IAuthService authService, IUserService userService, IConfiguration configuration)
        {
            _authService = authService;
            _userService = userService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);

            if (!result.Success)
            {
                return Ok(ApiResponse.Fail(result.ErrorMessage!));
            }

            WriteAuthCookies(result.Data!);

            return Ok(ApiResponse.Ok("Đăng nhập thành công"));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
        {
            var token = Request.Cookies[RefreshTokenCookie] ?? request?.RefreshToken;

            var result = await _authService.RefreshAsync(token ?? "", cancellationToken);

            if (!result.Success)
            {
                ClearAuthCookies();

                return Unauthorized(ApiResponse.Fail(result.ErrorMessage!));
            }

            WriteAuthCookies(result.Data!);

            return Ok(ApiResponse.Ok("Đã làm mới phiên đăng nhập"));
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(Request.Cookies[RefreshTokenCookie], cancellationToken);

            ClearAuthCookies();

            return Ok(ApiResponse.Ok("Đăng xuất thành công"));
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var response = await _userService.ChangeOwnPasswordAsync(request, cancellationToken);

            ClearAuthCookies();

            return Ok(response);
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
        {
            return Ok(await _authService.GetAllUsersAsync(cancellationToken));
        }

        // Cookie

        private void WriteAuthCookies(LoginResponse data)
        {
            var secure = _configuration.GetValue("COOKIESECURE", false)
                         || _configuration.GetValue("Security:CookieSecure", false);

            Response.Cookies.Append(AccessTokenCookie, data.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Expires = data.AccessTokenExpiration
            });

            Response.Cookies.Append(RefreshTokenCookie, data.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Path = RefreshCookiePath,
                Expires = data.RefreshTokenExpiration
            });

            var userInfoJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                userId = data.UserId,
                fullName = data.FullName,
                roles = data.Roles
            });

            Response.Cookies.Append(UserInfoCookie, userInfoJson, new CookieOptions
            {
                HttpOnly = false,          
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Expires = data.RefreshTokenExpiration
            });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete(AccessTokenCookie);
            Response.Cookies.Delete(UserInfoCookie);

            Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = RefreshCookiePath });
        }
    }
}
