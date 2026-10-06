namespace TonKhoApp.Domain.Entities
{
    public class StockTransaction
    {
        public long Id { get; set; }

        public int ProductId { get; set; }

        // IN / OUT / ADJUST / REVERSAL
        public string TransactionType { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }

        public decimal? UnitPrice { get; set; }

        public string? RefType { get; set; }
        public int? RefId { get; set; }

        public long? OriginalTransactionId { get; set; }

        public DateTime TransactionDate { get; set; }

        public int? CreatedBy { get; set; }

        public string? Note { get; set; }

        // Navigation properties
        public Product? Product { get; set; }
        public User? CreatedByUser { get; set; }
        public StockTransaction? OriginalTransaction { get; set; }
    }
}
