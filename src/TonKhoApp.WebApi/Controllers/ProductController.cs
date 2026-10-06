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
    [Route("api/products")]
    [Authorize] 
    public class ProductApiController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductApiController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
        {
            return Ok(await _productService.GetAllProductsAsync(pagination, cancellationToken));
        }

        // GET /api/products/{itemCode}
        [HttpGet("{itemCode}")]
        public async Task<IActionResult> GetProductDetail(string itemCode, CancellationToken cancellationToken)
        {
            return Ok(await _productService.GetProductDetailAsync(itemCode, cancellationToken));
        }

        [HttpPost]
        [Authorize(Roles = RoleCode.ManageCatalog)] // Admin + Nhân viên mua hàng
        [PermissionName("thêm sản phẩm")]
        public async Task<IActionResult> CreateProduct(
            [FromBody] ProductRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _productService.CreateProductAsync(request, cancellationToken));
        }

        [HttpPut("{itemCode}")]
        [Authorize(Roles = RoleCode.ManageCatalog)]
        [PermissionName("sửa sản phẩm")]
        public async Task<IActionResult> UpdateProduct(
            string itemCode, [FromBody] ProductRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _productService.UpdateProductAsync(itemCode, request, cancellationToken));
        }

        [HttpDelete("{itemCode}")]
        [Authorize(Roles = RoleCode.ManageCatalog)]
        [PermissionName("xoá sản phẩm")]
        public async Task<IActionResult> DeleteProduct(string itemCode, CancellationToken cancellationToken)
        {
            return Ok(await _productService.DeleteProductAsync(itemCode, cancellationToken));
        }
    }
}
