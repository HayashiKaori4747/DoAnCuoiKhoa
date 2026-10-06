namespace TonKhoApp.Domain.Constants
{
    public static class StockTransactionType
    {
        public const string In = "IN";              // Nhập kho  (Quantity > 0)
        public const string Out = "OUT";            // Xuất kho  (Quantity < 0)
        public const string Adjust = "ADJUST";      // Điều chỉnh/kiểm kê (Quantity ±)

        public const string Reversal = "REVERSAL";  

        public static readonly IReadOnlyList<string> All = new[] { In, Out, Adjust, Reversal };

        public static bool IsValid(string? type) => type != null && All.Contains(type);

        public static string GetText(string? type) => type switch
        {
            In => "Nhập kho",
            Out => "Xuất kho",
            Adjust => "Điều chỉnh",
            Reversal => "Bút toán đảo",
            _ => type ?? string.Empty
        };
    }

    public static class StockRefType
    {
        public const string Order = "ORDER";        // RefId = Orders.Id
        public const string Manual = "MANUAL";      // Nhập/xuất/điều chỉnh thủ công

        public const string Reversal = "REVERSAL";

        public static readonly IReadOnlyList<string> All = new[] { Order, Manual, Reversal };
    }
}
