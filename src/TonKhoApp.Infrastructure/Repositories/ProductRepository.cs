using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (activeOnly)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLike(search.Trim())}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.ItemCode, pattern) ||
                    EF.Functions.Like(p.ItemName, pattern));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(p => p.ItemCode) // Sắp xếp theo mã SP
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Product?> GetByItemCodeAsync(
            string itemCode, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.ItemCode == itemCode, cancellationToken);
        }

        public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products.FindAsync(new object?[] { id }, cancellationToken);
        }

        public async Task<bool> ExistsByItemCodeAsync(
            string itemCode, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AnyAsync(p => p.ItemCode == itemCode, cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products.CountAsync(cancellationToken);
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            await _context.Products.AddAsync(product, cancellationToken);
        }

        public void Remove(Product product)
        {
            _context.Products.Remove(product);
        }

        private static string EscapeLike(string value)
            => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
