using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IOrderService
    {
        Task<PagedResponse<OrderListItemDto>> GetAllOrdersAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default);

        Task<SingleResponse<OrderDetailDto>> GetOrderDetailAsync(
            string orderNo, CancellationToken cancellationToken = default);

        Task<ApiResponse> CreateOrderAsync(
            CreateOrderRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> UpdateOrderAsync(
            string orderNo, UpdateOrderRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteOrderAsync(
            string orderNo, CancellationToken cancellationToken = default);

        Task<ApiResponse> CancelOrderAsync(
            string orderNo, CancelOrderRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> ConfirmOrderAsync(
            string orderNo, CancellationToken cancellationToken = default);

        Task<ApiResponse> ConfirmStockAsync(
            string orderNo, CancellationToken cancellationToken = default);
    }
}
