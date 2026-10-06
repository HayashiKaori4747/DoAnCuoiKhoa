using TonKhoApp.Application.ReadModels;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<(IReadOnlyList<OrderListRow> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default);
        Task<Order?> GetByOrderNoWithDetailsAsync(string orderNo, CancellationToken cancellationToken = default);

        Task<Order?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);
        Task<long> GetNextOrderSequenceAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RecentOrderRow>> GetRecentOrdersAsync(
            int count, CancellationToken cancellationToken = default);

        Task<bool> ExistsByVendorIdAsync(int vendorId, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<string, int>> CountByStatusAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(Order order, CancellationToken cancellationToken = default);

        void Remove(Order order);

        Task<bool> IsProductUsedInOrdersAsync(int productId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OrderProduct>> GetOrderProductsByOrderIdAsync(
            int orderId, CancellationToken cancellationToken = default);

        Task AddOrderProductAsync(OrderProduct orderProduct, CancellationToken cancellationToken = default);

        void RemoveOrderProducts(IEnumerable<OrderProduct> orderProducts);
    }
}
