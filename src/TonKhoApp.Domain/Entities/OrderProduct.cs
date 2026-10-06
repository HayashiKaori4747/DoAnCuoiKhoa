namespace TonKhoApp.Domain.Entities
{
    public class OrderProduct
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 0;

        public decimal UnitPrice { get; set; } = 0;

        public decimal LineAmount { get; private set; }

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
