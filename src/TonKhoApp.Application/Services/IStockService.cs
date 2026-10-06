using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IStockService
    {
        Task<PagedResponse<StockDto>> GetAllStockAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default);

        Task<ApiResponse> IssueStockAsync(
            StockIssueRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> AdjustStockAsync(
            StockAdjustRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> ReverseTransactionAsync(
            StockReversalRequest request, CancellationToken cancellationToken = default);

        Task<PagedResponse<StockTransactionDto>> GetTransactionsAsync(
            string itemCode, PaginationRequest pagination, CancellationToken cancellationToken = default);
    }
}
