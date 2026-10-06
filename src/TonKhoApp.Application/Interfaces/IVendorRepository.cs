using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IVendorRepository
    {
        Task<(IReadOnlyList<Vendor> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, bool activeOnly = false,
            CancellationToken cancellationToken = default);

        Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Vendor?> GetByVendorCodeAsync(string vendorCode, CancellationToken cancellationToken = default);

        Task<Vendor?> GetByVendorNameAsync(string vendorName, CancellationToken cancellationToken = default);

        Task<bool> ExistsByVendorNameAsync(string vendorName, CancellationToken cancellationToken = default);

        Task<bool> ExistsByVendorCodeAsync(string vendorCode, CancellationToken cancellationToken = default);

        Task<string> GenerateNextVendorCodeAsync(CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);

        void Remove(Vendor vendor);
    }
}
