using TonKhoApp.Domain.Common;

namespace TonKhoApp.Domain.Entities
{
    public class User : IAuditable, ISoftDeletable
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty; // Tên đăng nhập

        public string UserPassword { get; set; } = string.Empty;

        public string? FullName { get; set; } 

        public ICollection<UserRole>? UserRoles { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Xoá mềm
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public ICollection<RefreshToken>? RefreshTokens { get; set; }
    }
}
