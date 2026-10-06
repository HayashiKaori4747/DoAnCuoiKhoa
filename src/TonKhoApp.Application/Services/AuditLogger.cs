using System.Text.Json;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUser _currentUser;

        public AuditLogger(IAuditLogRepository auditLogRepository, ICurrentUser currentUser)
        {
            _auditLogRepository = auditLogRepository;
            _currentUser = currentUser;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task LogAsync(
            string entityName,
            string? entityId,
            string action,
            object? before = null,
            object? after = null,
            string? description = null,
            int? actorId = null,
            string? actorName = null,
            CancellationToken cancellationToken = default)
        {
            await _auditLogRepository.AddAsync(new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),

                UserId = actorId ?? _currentUser.Id,
                UserName = actorName ?? _currentUser.UserName,
                IpAddress = _currentUser.IpAddress,

                Description = description,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        private static string? Serialize(object? value)
            => value == null ? null : JsonSerializer.Serialize(value, JsonOptions);
    }
}
