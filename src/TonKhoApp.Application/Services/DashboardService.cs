using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;

namespace TonKhoApp.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IStockRepository _stockRepository;

        public DashboardService(
            IUserRepository userRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IVendorRepository vendorRepository,
            IStockRepository stockRepository)
        {
            _userRepository = userRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _vendorRepository = vendorRepository;
            _stockRepository = stockRepository;
        }

        public async Task<SingleResponse<DashboardData>> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {

            var totalUsers = await _userRepository.CountAsync(cancellationToken);
            var totalProducts = await _productRepository.CountAsync(cancellationToken);
            var totalOrders = await _orderRepository.CountAsync(cancellationToken);
            var totalVendors = await _vendorRepository.CountAsync(cancellationToken);

            var byStatus = await _orderRepository.CountByStatusAsync(cancellationToken);

            var totalStockValue = await _stockRepository.GetTotalStockValueAsync(cancellationToken);

            var topStocks = await _stockRepository.GetTopByQuantityAsync(5, cancellationToken);
            var topStockItems = topStocks.Select(s => new TopStockItem
            {
                ItemCode = s.Product != null ? s.Product.ItemCode : "",
                ItemName = s.Product != null ? s.Product.ItemName : "",
                Quantity = s.Quantity
            }).ToList();

            var lowStock = await _stockRepository.GetBelowMinimumAsync(10, cancellationToken);
            var lowStockItems = lowStock.Select(s => new LowStockItem
            {
                ItemCode = s.ItemCode,
                ItemName = s.ItemName,
                Unit = s.Unit,
                Quantity = s.Quantity,
                MinQuantity = s.MinQuantity,
                Shortage = s.MinQuantity - s.Quantity
            }).ToList();

            var recent = await _orderRepository.GetRecentOrdersAsync(5, cancellationToken);
            var recentOrders = recent.Select(o => new RecentOrderItem
            {
                OrderNo = o.OrderNo,
                VendorName = o.VendorName,
                OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                Status = o.Status,
                StatusText = OrderStatus.GetText(o.Status),
                TotalAmount = o.TotalAmount
            }).ToList();

            var dashboardData = new DashboardData
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalVendors = totalVendors,
                TotalStockValue = totalStockValue,
                OrdersDraft = byStatus.GetValueOrDefault(OrderStatus.Draft),
                OrdersConfirmed = byStatus.GetValueOrDefault(OrderStatus.Confirmed),
                OrdersStocked = byStatus.GetValueOrDefault(OrderStatus.Received),
                OrdersCancelled = byStatus.GetValueOrDefault(OrderStatus.Cancelled),
                TopStockItems = topStockItems,
                LowStockItems = lowStockItems,
                RecentOrders = recentOrders
            };

            return new SingleResponse<DashboardData> { Success = true, Data = dashboardData };
        }
    }
}
