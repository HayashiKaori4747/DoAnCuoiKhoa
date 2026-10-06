namespace TonKhoApp.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }

       
        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }

        public int? UserId { get; set; }
        public string? UserName { get; set; }

        public string? IpAddress { get; set; }

        
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public User? User { get; set; }
    }
}
