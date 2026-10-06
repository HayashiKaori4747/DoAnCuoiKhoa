namespace TonKhoApp.Domain.Constants
{
    // MÃ vai trò 
    public static class RoleCode
    {
        public const string Admin = "Admin";            // Quản trị viên - toàn quyền
        public const string User = "User";              // Nhân viên - chỉ xem
        public const string Warehouse = "Warehouse";    // Nhân viên kho
        public const string Purchasing = "Purchasing";  // Nhân viên mua hàng

        public static readonly IReadOnlyList<string> All =
            new[] { Admin, User, Warehouse, Purchasing };

        public static bool IsValid(string? role) => role != null && All.Contains(role);

        public const string ManageCatalog = Admin + "," + Purchasing;

        public const string EditOrder = Admin + "," + Purchasing;

        public const string ConfirmOrder = Admin + "," + Purchasing;

        public const string ManageStock = Admin + "," + Warehouse;

        public const string AdminOnly = Admin;
    }
}
