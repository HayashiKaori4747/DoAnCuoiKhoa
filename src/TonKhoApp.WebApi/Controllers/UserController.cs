using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Services;
using TonKhoApp.Domain.Constants;
using TonKhoApp.WebApi.Middleware;

namespace TonKhoApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = RoleCode.AdminOnly)]
    [PermissionName("quản lý tài khoản")]
    public class UserApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserApiController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetUsersAsync(pagination, cancellationToken));
        }

        // GET /api/users/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetUserAsync(id, cancellationToken));
        }

        [HttpPost]
        [PermissionName("tạo tài khoản")]
        public async Task<IActionResult> CreateUser(
            [FromBody] UserCreateRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _userService.CreateUserAsync(request, cancellationToken));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(
            int id, [FromBody] UserUpdateRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _userService.UpdateUserAsync(id, request, cancellationToken));
        }

        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> ResetPassword(
            int id, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _userService.ResetPasswordAsync(id, request, cancellationToken));
        }
    }
}
