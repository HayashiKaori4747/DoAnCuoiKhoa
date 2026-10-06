using System.ComponentModel.DataAnnotations;

namespace TonKhoApp.Application.DTOs
{
    public class VendorDto
    {
        public int Id { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class VendorRequest
    {
        [MaxLength(50)]
        public string? VendorCode { get; set; }

        [Required(ErrorMessage = "Tên NCC không được để trống")]
        [MaxLength(255)]
        public string VendorName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        public bool? IsActive { get; set; }
    }
}
