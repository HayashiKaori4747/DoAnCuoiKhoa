using System.ComponentModel.DataAnnotations;

namespace TonKhoApp.Application.DTOs
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Nhà cung cấp không được để trống")]
        public string VendorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày đặt hàng không được để trống")]
        public string OrderDate { get; set; } = string.Empty;

        public string? Handler { get; set; }

        public string? Note { get; set; }

        [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
        public List<OrderProductItem> OrderProducts { get; set; } = new();
    }

    public class UpdateOrderRequest
    {
        [Required(ErrorMessage = "Nhà cung cấp không được để trống")]
        public string VendorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày đặt hàng không được để trống")]
        public string OrderDate { get; set; } = string.Empty;

        public string? Handler { get; set; }

        public string? Note { get; set; }

        [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
        public List<OrderProductItem> OrderProducts { get; set; } = new();
    }

    public class CancelOrderRequest
    {
        [Required(ErrorMessage = "Lý do huỷ không được để trống")]
        [MinLength(5, ErrorMessage = "Lý do huỷ phải có ít nhất 5 ký tự")]
        [MaxLength(500, ErrorMessage = "Lý do huỷ tối đa 500 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }

    public class OrderProductItem
    {
        [Required]
        public string ItemCode { get; set; } = string.Empty;

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        public decimal? UnitPrice { get; set; }
    }

    // JSON: { orderNo, vendorName, orderDate, handler, handlerName, totalAmount, status, statusText }
    public class OrderListItemDto
    {
        public string OrderNo { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string Handler { get; set; } = string.Empty;      
        public string HandlerName { get; set; } = string.Empty;  
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;       
        public string StatusText { get; set; } = string.Empty;  

        public string? CancelReason { get; set; }
    }

    public class OrderDetailDto
    {
        public string OrderNo { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string Handler { get; set; } = string.Empty;
        public string HandlerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        public string? CancelReason { get; set; }
        public string? CancelledAt { get; set; }    
        public string? CancelledByName { get; set; }

        public List<OrderProductLineDto> OrderProducts { get; set; } = new();

        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class OrderStatusHistoryDto
    {
        public string? FromStatus { get; set; }
        public string? FromStatusText { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string ToStatusText { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
        public string ChangedAt { get; set; } = string.Empty;   
    }

    // JSON: { itemCode, itemName, unit, quantity, unitPrice, lineAmount }
    public class OrderProductLineDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }
    }

    public class OrderNoDto
    {
        public string OrderNo { get; set; } = string.Empty;
    }
}
