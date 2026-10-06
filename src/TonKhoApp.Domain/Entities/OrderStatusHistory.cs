namespace TonKhoApp.Domain.Entities
{
    public class OrderStatusHistory
    {
        public long Id { get; set; }

        public int OrderId { get; set; }
        public string? FromStatus { get; set; }

        public string ToStatus { get; set; } = string.Empty;

        public string? Reason { get; set; }

        public int? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public User? ChangedByUser { get; set; }
    }
}
