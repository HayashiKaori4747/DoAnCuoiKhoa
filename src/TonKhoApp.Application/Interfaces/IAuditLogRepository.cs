using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? entityName,
            string? entityId,
            int? userId,
            string? action,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default);
    }
}
