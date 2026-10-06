using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IOrderStatusHistoryRepository
    {
        Task AddAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OrderStatusHistory>> GetByOrderIdAsync(
            int orderId, CancellationToken cancellationToken = default);
    }
}
