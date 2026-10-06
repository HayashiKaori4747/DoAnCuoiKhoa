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
    [Route("api/orders")]
    [Authorize]
    public class OrderApiController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderApiController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.GetAllOrdersAsync(pagination, cancellationToken));
        }

        // GET /api/orders/{orderNo}
        [HttpGet("{orderNo}")]
        public async Task<IActionResult> GetOrderDetail(string orderNo, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.GetOrderDetailAsync(orderNo, cancellationToken));
        }

        // POST /api/orders
        [HttpPost]
        [Authorize(Roles = RoleCode.EditOrder)] 
        [PermissionName("lập đơn mua hàng")]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.CreateOrderAsync(request, cancellationToken));
        }

        // PUT /api/orders/{orderNo}
        [HttpPut("{orderNo}")]
        [Authorize(Roles = RoleCode.EditOrder)] 
        [PermissionName("sửa đơn hàng")]
        public async Task<IActionResult> UpdateOrder(
            string orderNo, [FromBody] UpdateOrderRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.UpdateOrderAsync(orderNo, request, cancellationToken));
        }

        [HttpDelete("{orderNo}")]
        [Authorize(Roles = RoleCode.EditOrder)] 
        [PermissionName("xoá đơn hàng")]
        public async Task<IActionResult> DeleteOrder(string orderNo, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.DeleteOrderAsync(orderNo, cancellationToken));
        }

        [HttpPut("{orderNo}/cancel")]
        [Authorize(Roles = RoleCode.EditOrder)] 
        [PermissionName("huỷ đơn hàng")]
        public async Task<IActionResult> CancelOrder(
            string orderNo, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.CancelOrderAsync(orderNo, request, cancellationToken));
        }

        // PUT /api/orders/{orderNo}/confirm
        [HttpPut("{orderNo}/confirm")]
        [Authorize(Roles = RoleCode.ConfirmOrder)] // Admin + Nhân viên mua hàng
        [PermissionName("xác nhận đơn hàng")]
        public async Task<IActionResult> ConfirmOrder(string orderNo, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.ConfirmOrderAsync(orderNo, cancellationToken));
        }

        // PUT /api/orders/{orderNo}/stock
        [HttpPut("{orderNo}/stock")]
        [Authorize(Roles = RoleCode.ManageStock)] // Admin + Nhân viên kho
        [PermissionName("nhập kho")]
        public async Task<IActionResult> ConfirmStock(string orderNo, CancellationToken cancellationToken)
        {
            return Ok(await _orderService.ConfirmStockAsync(orderNo, cancellationToken));
        }
    }
}
