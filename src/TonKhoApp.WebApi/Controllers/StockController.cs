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
    [Route("api/stock")]
    [Authorize]
    public class StockApiController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockApiController(IStockService stockService)
        {
            _stockService = stockService;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAllStock(
            [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
        {
            return Ok(await _stockService.GetAllStockAsync(pagination, cancellationToken));
        }

        [HttpGet("{itemCode}/transactions")]
        public async Task<IActionResult> GetTransactions(
            string itemCode, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
        {
            return Ok(await _stockService.GetTransactionsAsync(itemCode, pagination, cancellationToken));
        }

        // POST /api/stock/issue - xuất kho thủ công
        [HttpPost("issue")]
        [Authorize(Roles = RoleCode.ManageStock)]
        [PermissionName("xuất kho")]
        public async Task<IActionResult> IssueStock(
            [FromBody] StockIssueRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _stockService.IssueStockAsync(request, cancellationToken));
        }

        // POST /api/stock/adjust - điều chỉnh tồn theo số kiểm kê thực tế
        [HttpPost("adjust")]
        [Authorize(Roles = RoleCode.ManageStock)]
        [PermissionName("điều chỉnh tồn kho")]
        public async Task<IActionResult> AdjustStock(
            [FromBody] StockAdjustRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _stockService.AdjustStockAsync(request, cancellationToken));
        }

        [HttpPost("reverse")]
        [Authorize(Roles = RoleCode.ManageStock)]
        [PermissionName("đảo bút toán kho")]
        public async Task<IActionResult> ReverseTransaction(
            [FromBody] StockReversalRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _stockService.ReverseTransactionAsync(request, cancellationToken));
        }
    }
}
