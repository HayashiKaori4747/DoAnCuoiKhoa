using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Application.ReadModels;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly AppDbContext _context;

        public StockRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<StockListRow> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var query = _context.Stocks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLike(search.Trim())}%";
                query = query.Where(s =>
                    EF.Functions.Like(s.Product!.ItemCode, pattern) ||
                    EF.Functions.Like(s.Product!.ItemName, pattern));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(s => s.Product!.ItemCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StockListRow
                {
                    ItemCode = s.Product!.ItemCode,
                    ItemName = s.Product!.ItemName,
                    Unit = s.Product!.Unit,
                    Quantity = s.Quantity,
                    Price = s.Product!.Price,
                    StockValue = s.Quantity * s.Product!.Price,
                    MinQuantity = s.Product!.MinQuantity,
                    BelowMinimum = s.Product!.MinQuantity > 0 && s.Quantity < s.Product!.MinQuantity
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Stock?> GetByProductIdAsync(
            int productId, CancellationToken cancellationToken = default)
        {
            return await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        }

        public async Task<decimal> GetTotalStockValueAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Stocks
                .SumAsync(s => (decimal?)(s.Quantity * s.Product!.Price), cancellationToken) ?? 0m;
        }

        public async Task<IReadOnlyList<Stock>> GetTopByQuantityAsync(
            int count, CancellationToken cancellationToken = default)
        {
            return await _context.Stocks
                .AsNoTracking()
                .Include(s => s.Product)
                .OrderByDescending(s => s.Quantity)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Stock stock, CancellationToken cancellationToken = default)
        {
            await _context.Stocks.AddAsync(stock, cancellationToken);
        }

        public void Remove(Stock stock)
        {
            _context.Stocks.Remove(stock);
        }

        // Sổ cái kho

        public async Task AddTransactionAsync(
            StockTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.StockTransactions.AddAsync(transaction, cancellationToken);
        }

        public async Task<bool> HasOrderTransactionAsync(
            int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.StockTransactions.AnyAsync(
                t => t.RefType == StockRefType.Order && t.RefId == orderId,
                cancellationToken);
        }

        public async Task<bool> HasTransactionsAsync(
            int productId, CancellationToken cancellationToken = default)
        {
            return await _context.StockTransactions
                .AnyAsync(t => t.ProductId == productId, cancellationToken);
        }

        public async Task<(IReadOnlyList<StockLedgerRow> Items, int TotalCount)> GetTransactionsAsync(
            int productId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.StockTransactions
                .AsNoTracking()
                .Where(t => t.ProductId == productId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new StockLedgerRow
                {
                    Id = t.Id,
                    TransactionType = t.TransactionType,
                    Quantity = t.Quantity,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    UnitPrice = t.UnitPrice,
                    OriginalTransactionId = t.OriginalTransactionId,
                    RefType = t.RefType,
                    RefId = t.RefId,

                    RefNo = t.RefType == StockRefType.Order
                        ? _context.Orders
                            .IgnoreQueryFilters()   
                            .Where(o => o.Id == t.RefId)
                            .Select(o => o.OrderNo)
                            .FirstOrDefault()
                        : null,

                    TransactionDate = t.TransactionDate,

                    CreatedByName = _context.Users
                        .IgnoreQueryFilters()
                        .Where(u => u.Id == t.CreatedBy)
                        .Select(u => u.FullName ?? u.UserId)
                        .FirstOrDefault(),

                    Note = t.Note
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<decimal> GetLedgerBalanceAsync(
            int productId, CancellationToken cancellationToken = default)
        {
            return await _context.StockTransactions
                .Where(t => t.ProductId == productId)
                .SumAsync(t => (decimal?)t.Quantity, cancellationToken) ?? 0m;
        }

        // BÚT TOÁN ĐẢO

        public async Task<StockTransaction?> GetTransactionByIdAsync(
            long id, CancellationToken cancellationToken = default)
        {
            return await _context.StockTransactions
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<bool> HasReversalAsync(
            long originalTransactionId, CancellationToken cancellationToken = default)
        {
            return await _context.StockTransactions
                .AnyAsync(t => t.OriginalTransactionId == originalTransactionId, cancellationToken);
        }

        public async Task<IReadOnlyList<StockListRow>> GetBelowMinimumAsync(
            int maxRows, CancellationToken cancellationToken = default)
        {
            var query =
                from p in _context.Products
                join st in _context.Stocks on p.Id equals st.ProductId into g
                from s in g.DefaultIfEmpty()
                let qty = s == null ? 0m : s.Quantity
                where p.IsActive && p.MinQuantity > 0 && qty < p.MinQuantity
                orderby p.MinQuantity - qty descending
                select new StockListRow
                {
                    ItemCode = p.ItemCode,
                    ItemName = p.ItemName,
                    Unit = p.Unit,
                    Quantity = qty,
                    Price = p.Price,
                    StockValue = qty * p.Price,
                    MinQuantity = p.MinQuantity,
                    BelowMinimum = true
                };

            return await query.AsNoTracking().Take(maxRows).ToListAsync(cancellationToken);
        }

        private static string EscapeLike(string value)
            => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
