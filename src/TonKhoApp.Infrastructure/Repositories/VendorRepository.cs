using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly AppDbContext _context;

        public VendorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<Vendor> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Vendors.AsNoTracking().AsQueryable();

            if (activeOnly)
            {
                query = query.Where(v => v.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLike(search.Trim())}%";
                query = query.Where(v =>
                    EF.Functions.Like(v.VendorCode, pattern) ||
                    EF.Functions.Like(v.VendorName, pattern) ||
                    (v.Address != null && EF.Functions.Like(v.Address, pattern)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(v => v.VendorName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Vendors.FindAsync(new object?[] { id }, cancellationToken);
        }

        public async Task<Vendor?> GetByVendorCodeAsync(
            string vendorCode, CancellationToken cancellationToken = default)
        {
            return await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorCode == vendorCode, cancellationToken);
        }

        public async Task<Vendor?> GetByVendorNameAsync(
            string vendorName, CancellationToken cancellationToken = default)
        {
            return await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorName == vendorName, cancellationToken);
        }

        public async Task<bool> ExistsByVendorNameAsync(
            string vendorName, CancellationToken cancellationToken = default)
        {
            return await _context.Vendors
                .AnyAsync(v => v.VendorName == vendorName, cancellationToken);
        }

        public async Task<bool> ExistsByVendorCodeAsync(
            string vendorCode, CancellationToken cancellationToken = default)
        {
            return await _context.Vendors
                .AnyAsync(v => v.VendorCode == vendorCode, cancellationToken);
        }

        public async Task<string> GenerateNextVendorCodeAsync(CancellationToken cancellationToken = default)
        {
            var next = await SequenceReader.NextValueAsync(_context, "Seq_VendorCode", cancellationToken);
            return "NCC" + next.ToString("D3");
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Vendors.CountAsync(cancellationToken);
        }

        public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
        {
            await _context.Vendors.AddAsync(vendor, cancellationToken);
        }

        public void Remove(Vendor vendor)
        {
            _context.Vendors.Remove(vendor);
        }

        private static string EscapeLike(string value)
            => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
