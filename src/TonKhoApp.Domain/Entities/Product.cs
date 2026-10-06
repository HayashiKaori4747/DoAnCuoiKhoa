using TonKhoApp.Domain.Common;

namespace TonKhoApp.Domain.Entities
{
    public class Product : IAuditable, ISoftDeletable
    {
        public int Id { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Unit { get; set; } = "kg";

        public decimal Price { get; set; } = 0;

        public decimal MinQuantity { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Xoá mềm 
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public ICollection<OrderProduct>? OrderProducts { get; set; } 
        public ICollection<StockTransaction>? StockTransactions { get; set; } 
        public Stock? Stock { get; set; } 
    }
}
