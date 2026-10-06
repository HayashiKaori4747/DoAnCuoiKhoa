using System.ComponentModel.DataAnnotations;

namespace TonKhoApp.Application.DTOs
{
    // JSON: { itemCode, itemName, unit, price, minQuantity, isActive }
    public class ProductDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = "kg";
        public decimal Price { get; set; }

        public decimal MinQuantity { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ProductRequest
    {
        [MaxLength(50, ErrorMessage = "Mã sản phẩm tối đa 50 ký tự")]
        public string ItemCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [MaxLength(255, ErrorMessage = "Tên sản phẩm tối đa 255 ký tự")]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự")]
        public string Unit { get; set; } = "kg";

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
        public decimal? Price { get; set; }

        
        [Range(0, double.MaxValue, ErrorMessage = "Định mức tồn tối thiểu không được âm")]
        public decimal? MinQuantity { get; set; }
        public bool? IsActive { get; set; }
    }
}
