namespace TonKhoApp.Domain.Entities
{
    public class RefreshToken
    {
        public long Id { get; set; }

        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedByIp { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? RevokedReason { get; set; }

        public long? ReplacedByTokenId { get; set; }

        // Navigation
        public User? User { get; set; }

        public RefreshToken? ReplacedByToken { get; set; }

        public bool IsActive(DateTime nowUtc) => RevokedAt == null && ExpiresAt > nowUtc;
    }

    public static class RevokeReason
    {
        public const string Logout = "LOGOUT";                   // người dùng tự đăng xuất
        public const string Rotated = "ROTATED";                 // đã đổi lấy token mới
        public const string AdminLock = "ADMIN_LOCK";            // Admin khoá tài khoản
        public const string ReuseDetected = "REUSE_DETECTED";    // phát hiện dùng lại token đã xoay
        public const string PasswordChanged = "PASSWORD_CHANGED";// đổi/đặt lại mật khẩu

        public static readonly IReadOnlyList<string> All =
            new[] { Logout, Rotated, AdminLock, ReuseDetected, PasswordChanged };
    }
}
