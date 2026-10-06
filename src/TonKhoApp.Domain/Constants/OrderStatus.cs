namespace TonKhoApp.Domain.Constants
{
    public static class OrderStatus
    {
        public const string Draft = "DRAFT";          // Bản nháp
        public const string Confirmed = "CONFIRMED";  // Đã xác nhận
        public const string Received = "RECEIVED";    // Đã nhập kho

        public const string Cancelled = "CANCELLED";

        public static readonly IReadOnlyList<string> All =
            new[] { Draft, Confirmed, Received, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
        private static readonly Dictionary<string, string[]> AllowedTransitions = new()
        {
            [Draft] = new[] { Confirmed, Cancelled },
            [Confirmed] = new[] { Received, Cancelled },
            [Received] = Array.Empty<string>(),
            [Cancelled] = Array.Empty<string>()
        };

        public static bool CanTransition(string? from, string to)
            => from != null
               && AllowedTransitions.TryGetValue(from, out var allowed)
               && allowed.Contains(to);

        public static bool IsFinal(string? status)
            => status == Received || status == Cancelled;

        public static string GetText(string? status) => status switch
        {
            Draft => "Bản nháp",
            Confirmed => "Đã xác nhận",
            Received => "Đã nhập kho",
            Cancelled => "Đã huỷ",
            _ => status ?? string.Empty
        };
    }
}
