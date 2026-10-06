using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Services;
using TonKhoApp.Domain.Constants;

namespace TonKhoApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = RoleCode.AdminOnly)]
    public class AuditLogApiController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogApiController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] AuditLogFilter filter, CancellationToken cancellationToken)
        {
            return Ok(await _auditLogService.GetLogsAsync(filter, cancellationToken));
        }

        [HttpGet("actions")]
        public IActionResult GetActions()
        {
            return Ok(new { success = true, data = AuditAction.All });
        }
    }
}
