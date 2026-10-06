using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, bool activeOnly = false,
            CancellationToken cancellationToken = default);

        Task<Product?> GetByItemCodeAsync(string itemCode, CancellationToken cancellationToken = default);
        Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByItemCodeAsync(string itemCode, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Product product, CancellationToken cancellationToken = default);

        void Remove(Product product);
    }
}
