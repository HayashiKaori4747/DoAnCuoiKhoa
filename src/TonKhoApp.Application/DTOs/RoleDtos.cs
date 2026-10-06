using System.ComponentModel.DataAnnotations;

namespace TonKhoApp.Application.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsSystem { get; set; }

        public int UserCount { get; set; }
    }

    public class RoleRequest
    {
        [Required(ErrorMessage = "Mã vai trò không được để trống")]
        [MaxLength(50, ErrorMessage = "Mã vai trò tối đa 50 ký tự")]
        [RegularExpression("^[A-Za-z][A-Za-z0-9_]*$",
            ErrorMessage = "Mã vai trò chỉ gồm chữ cái, chữ số và dấu gạch dưới, bắt đầu bằng chữ cái")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên vai trò tối đa 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }
    }
}
