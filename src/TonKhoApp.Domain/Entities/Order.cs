using TonKhoApp.Domain.Common;
using TonKhoApp.Domain.Constants;

namespace TonKhoApp.Domain.Entities
{
    public class Order : IAuditable, ISoftDeletable
    {
        public int Id { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public int VendorId { get; set; }

        public DateOnly OrderDate { get; set; }

        public int? HandlerUserId { get; set; }

        public string Status { get; set; } = OrderStatus.Draft;

        public string? Note { get; set; }

        public DateTime? CancelledAt { get; set; }
        public int? CancelledBy { get; set; }
        public string? CancelReason { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Vendor? Vendor { get; set; }
        public User? HandlerUser { get; set; }
        public User? CancelledByUser { get; set; }
        public ICollection<OrderProduct>? OrderProducts { get; set; }
        public ICollection<OrderStatusHistory>? StatusHistory { get; set; }
    }
}
