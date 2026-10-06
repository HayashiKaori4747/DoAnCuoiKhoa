namespace TonKhoApp.Application.DTOs
{
    public class AuditLogDto
    {
        public long Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }

        public string Action { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;

        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }

        public int? UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string? IpAddress { get; set; }
        public string? Description { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
    public class AuditLogFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }
}
