namespace TonKhoApp.Application.ReadModels
{
    public class StockListRow
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal StockValue { get; set; }

        public decimal MinQuantity { get; set; }
        public bool BelowMinimum { get; set; }
    }
}
