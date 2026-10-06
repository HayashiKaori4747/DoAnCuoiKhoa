using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Services;
using TonKhoApp.Domain.Constants;
using TonKhoApp.WebApi.Middleware;

namespace TonKhoApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = RoleCode.AdminOnly)]
    public class RoleApiController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleApiController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // GET /api/roles
        [HttpGet]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            return Ok(await _roleService.GetRolesAsync(cancellationToken));
        }

        // POST /api/roles
        [HttpPost]
        [PermissionName("tạo vai trò")]
        public async Task<IActionResult> CreateRole(
            [FromBody] RoleRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _roleService.CreateRoleAsync(request, cancellationToken));
        }

        // PUT /api/roles/{id}
        [HttpPut("{id:int}")]
        [PermissionName("sửa vai trò")]
        public async Task<IActionResult> UpdateRole(
            int id, [FromBody] RoleRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _roleService.UpdateRoleAsync(id, request, cancellationToken));
        }

        [HttpDelete("{id:int}")]
        [PermissionName("xoá vai trò")]
        public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
        {
            return Ok(await _roleService.DeleteRoleAsync(id, cancellationToken));
        }
    }
}
