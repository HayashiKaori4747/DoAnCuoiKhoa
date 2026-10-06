using TonKhoApp.Application.ReadModels;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IStockRepository
    {
        Task<(IReadOnlyList<StockListRow> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default);

        Task<Stock?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

        Task<decimal> GetTotalStockValueAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Stock>> GetTopByQuantityAsync(int count, CancellationToken cancellationToken = default);

        Task AddAsync(Stock stock, CancellationToken cancellationToken = default);

        void Remove(Stock stock);

        Task AddTransactionAsync(StockTransaction transaction, CancellationToken cancellationToken = default);

        Task<bool> HasOrderTransactionAsync(int orderId, CancellationToken cancellationToken = default);

        Task<bool> HasTransactionsAsync(int productId, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<StockLedgerRow> Items, int TotalCount)> GetTransactionsAsync(
            int productId, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<decimal> GetLedgerBalanceAsync(int productId, CancellationToken cancellationToken = default);

        Task<StockTransaction?> GetTransactionByIdAsync(
            long id, CancellationToken cancellationToken = default);

        Task<bool> HasReversalAsync(long originalTransactionId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StockListRow>> GetBelowMinimumAsync(
            int maxRows, CancellationToken cancellationToken = default);
    }
}
