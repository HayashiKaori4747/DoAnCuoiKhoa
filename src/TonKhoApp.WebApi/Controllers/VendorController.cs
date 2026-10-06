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
    [Route("api/vendors")]
    [Authorize]
    public class VendorApiController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public VendorApiController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        // GET /api/vendors
        [HttpGet]
        public async Task<IActionResult> GetAllVendors(
            [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
        {
            return Ok(await _vendorService.GetAllVendorsAsync(pagination, cancellationToken));
        }

        // GET /api/vendors/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVendorDetail(int id, CancellationToken cancellationToken)
        {
            return Ok(await _vendorService.GetVendorDetailAsync(id, cancellationToken));
        }

        // POST /api/vendors
        [HttpPost]
        [Authorize(Roles = RoleCode.ManageCatalog)]
        [PermissionName("thêm nhà cung cấp")]
        public async Task<IActionResult> CreateVendor(
            [FromBody] VendorRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _vendorService.CreateVendorAsync(request, cancellationToken));
        }

        // PUT /api/vendors/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = RoleCode.ManageCatalog)]
        [PermissionName("sửa nhà cung cấp")]
        public async Task<IActionResult> UpdateVendor(
            int id, [FromBody] VendorRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _vendorService.UpdateVendorAsync(id, request, cancellationToken));
        }

        // DELETE /api/vendors/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleCode.ManageCatalog)]
        [PermissionName("xoá nhà cung cấp")]
        public async Task<IActionResult> DeleteVendor(int id, CancellationToken cancellationToken)
        {
            return Ok(await _vendorService.DeleteVendorAsync(id, cancellationToken));
        }
    }
}
