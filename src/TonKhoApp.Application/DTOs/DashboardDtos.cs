namespace TonKhoApp.Application.DTOs
{
    public class DashboardData
    {
        public int TotalUsers { get; set; }          
        public int TotalProducts { get; set; }       
        public int TotalOrders { get; set; }         
        public int TotalVendors { get; set; }        
        public decimal TotalStockValue { get; set; } 

        public int OrdersDraft { get; set; }         
        public int OrdersConfirmed { get; set; }     
        public int OrdersStocked { get; set; }       
        public int OrdersCancelled { get; set; }     

        public List<TopStockItem> TopStockItems { get; set; } = new();

        public List<LowStockItem> LowStockItems { get; set; } = new();

        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public class LowStockItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal MinQuantity { get; set; }

        public decimal Shortage { get; set; }
    }

    public class TopStockItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class RecentOrderItem
    {
        public string OrderNo { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;      
        public string StatusText { get; set; } = string.Empty;  
        public decimal TotalAmount { get; set; }
    }
}
