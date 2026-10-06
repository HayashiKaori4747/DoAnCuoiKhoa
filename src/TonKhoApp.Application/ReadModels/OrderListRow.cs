namespace TonKhoApp.Application.ReadModels
{
    public class OrderListRow
    {
        public string OrderNo { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public string? HandlerUserName { get; set; }  // Users.UserId
        public string? HandlerFullName { get; set; }  // Users.FullName
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;

        public string? CancelReason { get; set; }
    }
}
