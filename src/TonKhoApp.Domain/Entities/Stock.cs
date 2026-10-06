using TonKhoApp.Domain.Common;

namespace TonKhoApp.Domain.Entities
{
    public class Stock : IAuditable
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public decimal Quantity { get; set; } = 0;

        public byte[]? RowVersion { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation property
        public Product? Product { get; set; }
    }
}
