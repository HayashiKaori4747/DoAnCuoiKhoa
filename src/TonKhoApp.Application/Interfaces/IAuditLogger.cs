using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IAuditLogger
    {
        Task LogAsync(
            string entityName,
            string? entityId,
            string action,
            object? before = null,
            object? after = null,
            string? description = null,
            int? actorId = null,
            string? actorName = null,
            CancellationToken cancellationToken = default);
    }
}
