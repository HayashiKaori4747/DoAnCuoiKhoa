using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TonKhoApp.Application.DTOs
{
    public class StockDto
    {
        [JsonPropertyName("item_CD")]
        public string ItemCd { get; set; } = string.Empty;

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("stock")]
        public decimal Stock { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("stockValue")]
        public decimal StockValue { get; set; }
        [JsonPropertyName("minQuantity")]
        public decimal MinQuantity { get; set; }

        [JsonPropertyName("belowMinimum")]
        public bool BelowMinimum { get; set; }
    }

    public class StockIssueRequest
    {
        [Required]
        public string ItemCode { get; set; } = string.Empty;

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng xuất phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class StockAdjustRequest
    {
        [Required]
        public string ItemCode { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Số lượng thực tế không được âm")]
        public decimal ActualQuantity { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class StockReversalRequest
    {
        [Range(1, long.MaxValue, ErrorMessage = "Phải chọn bút toán cần đảo")]
        public long TransactionId { get; set; }
        [Required(ErrorMessage = "Lý do đảo bút toán không được để trống")]
        [MinLength(5, ErrorMessage = "Lý do phải có ít nhất 5 ký tự")]
        [MaxLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }

    public class StockTransactionDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionTypeText { get; set; } = string.Empty;
        public decimal Quantity { get; set; }

        
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }

        public decimal? UnitPrice { get; set; }
        public string? RefType { get; set; }
        public int? RefId { get; set; }
        public string? RefNo { get; set; }
        public long? OriginalTransactionId { get; set; }

        public string TransactionDate { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
