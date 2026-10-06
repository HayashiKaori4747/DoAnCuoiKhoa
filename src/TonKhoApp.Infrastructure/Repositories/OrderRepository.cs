using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Application.ReadModels;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<OrderListRow> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var query = _context.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLike(search.Trim())}%";
                query = query.Where(o =>
                    EF.Functions.Like(o.OrderNo, pattern) ||
                    EF.Functions.Like(o.Vendor!.VendorName, pattern) ||
                    (o.HandlerUser != null && o.HandlerUser.FullName != null &&
                     EF.Functions.Like(o.HandlerUser.FullName, pattern)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderListRow
                {
                    OrderNo = o.OrderNo,
                    VendorName = o.Vendor!.VendorName,
                    OrderDate = o.OrderDate,
                    HandlerUserName = o.HandlerUser != null ? o.HandlerUser.UserId : null,
                    HandlerFullName = o.HandlerUser != null ? o.HandlerUser.FullName : null,
                    TotalAmount = o.OrderProducts!.Sum(op => (decimal?)op.LineAmount) ?? 0m,
                    Status = o.Status,
                    CancelReason = o.CancelReason
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Order?> GetByOrderNoWithDetailsAsync(
            string orderNo, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Vendor)
                .Include(o => o.HandlerUser)
                .Include(o => o.CancelledByUser)
                .Include(o => o.OrderProducts!)
                .FirstOrDefaultAsync(o => o.OrderNo == orderNo, cancellationToken);
        }

        public async Task<Order?> GetByOrderNoAsync(
            string orderNo, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNo == orderNo, cancellationToken);
        }

        public Task<long> GetNextOrderSequenceAsync(CancellationToken cancellationToken = default)
            => SequenceReader.NextValueAsync(_context, "Seq_OrderNo", cancellationToken);

        public async Task<IReadOnlyList<RecentOrderRow>> GetRecentOrdersAsync(
            int count, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .Take(count)
                .Select(o => new RecentOrderRow
                {
                    OrderNo = o.OrderNo,
                    VendorName = o.Vendor!.VendorName,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.OrderProducts!.Sum(op => (decimal?)op.LineAmount) ?? 0m
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByVendorIdAsync(
            int vendorId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AnyAsync(o => o.VendorId == vendorId, cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Orders.CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<string, int>> CountByStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(r => r.Status, r => r.Count);
        }

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _context.Orders.AddAsync(order, cancellationToken);
        }

        public void Remove(Order order)
        {
            _context.Orders.Remove(order);
        }

        // Bảng OrderProducts

        public async Task<bool> IsProductUsedInOrdersAsync(
            int productId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderProducts
                .AnyAsync(op => op.ProductId == productId, cancellationToken);
        }

        public async Task<IReadOnlyList<OrderProduct>> GetOrderProductsByOrderIdAsync(
            int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderProducts
                .Where(op => op.OrderId == orderId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddOrderProductAsync(
            OrderProduct orderProduct, CancellationToken cancellationToken = default)
        {
            await _context.OrderProducts.AddAsync(orderProduct, cancellationToken);
        }

        public void RemoveOrderProducts(IEnumerable<OrderProduct> orderProducts)
        {
            _context.OrderProducts.RemoveRange(orderProducts);
        }

        private static string EscapeLike(string value)
            => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
