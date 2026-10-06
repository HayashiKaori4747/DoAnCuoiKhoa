using TonKhoApp.Domain.Common;

namespace TonKhoApp.Domain.Entities
{
    public class Vendor : IAuditable, ISoftDeletable
    {
        public int Id { get; set; } 
        public string VendorCode { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? TaxCode { get; set; } // Mã số thuế

        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Xoá mềm 
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation property
        public ICollection<Order>? Orders { get; set; }
    }
}
