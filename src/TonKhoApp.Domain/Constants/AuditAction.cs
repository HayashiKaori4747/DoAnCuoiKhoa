namespace TonKhoApp.Domain.Constants
{
    public static class AuditAction
    {
        public const string Create = "CREATE";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";                  // xoá mềm hoặc xoá cứng
        public const string PriceChange = "PRICE_CHANGE";       // sửa giá sản phẩm
        public const string Lock = "LOCK";                      // khoá tài khoản
        public const string Unlock = "UNLOCK";                  // mở khoá tài khoản
        public const string RoleGrant = "ROLE_GRANT";           // cấp vai trò
        public const string RoleRevoke = "ROLE_REVOKE";         // gỡ vai trò
        public const string PasswordReset = "PASSWORD_RESET";   // Admin đặt lại hộ
        public const string PasswordChange = "PASSWORD_CHANGE"; // người dùng tự đổi
        public const string Login = "LOGIN";
        public const string LoginFailed = "LOGIN_FAILED";
        public const string Logout = "LOGOUT";
        public const string OrderConfirm = "ORDER_CONFIRM";
        public const string OrderReceive = "ORDER_RECEIVE";     // nhập kho theo đơn
        public const string OrderCancel = "ORDER_CANCEL";
        public const string StockAdjust = "STOCK_ADJUST";       // điều chỉnh theo kiểm kê
        public const string StockIssue = "STOCK_ISSUE";         // xuất kho thủ công
        public const string StockReversal = "STOCK_REVERSAL";   // bút toán đảo

        public static readonly IReadOnlyList<string> All = new[]
        {
            Create, Update, Delete, PriceChange, Lock, Unlock, RoleGrant, RoleRevoke,
            PasswordReset, PasswordChange, Login, LoginFailed, Logout,
            OrderConfirm, OrderReceive, OrderCancel,
            StockAdjust, StockIssue, StockReversal
        };
    }
}
