namespace TonKhoApp.Application.ReadModels
{
    public class StockLedgerRow
    {
        public long Id { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }

        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal? UnitPrice { get; set; }

        public string? RefType { get; set; }
        public int? RefId { get; set; }

        // Mã phiếu 
        public string? RefNo { get; set; }

        public long? OriginalTransactionId { get; set; }

        public DateTime TransactionDate { get; set; }
        public string? CreatedByName { get; set; }
        public string? Note { get; set; }
    }
}
