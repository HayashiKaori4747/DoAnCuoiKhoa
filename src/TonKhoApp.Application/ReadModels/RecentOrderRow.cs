namespace TonKhoApp.Application.ReadModels
{
    public class RecentOrderRow
    {
        public string OrderNo { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
