using TonKhoApp.Domain.Common;

namespace TonKhoApp.Domain.Entities
{
    public class Role : IAuditable
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsSystem { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public ICollection<UserRole>? UserRoles { get; set; }
    }
}
