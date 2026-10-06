using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
    {
        private readonly AppDbContext _context;

        public OrderStatusHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            OrderStatusHistory history, CancellationToken cancellationToken = default)
        {
            await _context.OrderStatusHistory.AddAsync(history, cancellationToken);
        }

        public async Task<IReadOnlyList<OrderStatusHistory>> GetByOrderIdAsync(
            int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderStatusHistory
                .AsNoTracking()
                .Include(h => h.ChangedByUser)
                .IgnoreQueryFilters()
                .Where(h => h.OrderId == orderId)
                .OrderBy(h => h.ChangedAt)
                .ThenBy(h => h.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
