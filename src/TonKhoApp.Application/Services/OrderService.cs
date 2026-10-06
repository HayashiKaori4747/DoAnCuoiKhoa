using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusHistoryRepository _historyRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderStatusHistoryRepository historyRepository,
            IVendorRepository vendorRepository,
            IProductRepository productRepository,
            IStockRepository stockRepository,
            IUserRepository userRepository,
            IAuditLogger auditLogger,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _orderRepository = orderRepository;
            _historyRepository = historyRepository;
            _vendorRepository = vendorRepository;
            _productRepository = productRepository;
            _stockRepository = stockRepository;
            _userRepository = userRepository;
            _auditLogger = auditLogger;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<PagedResponse<OrderListItemDto>> GetAllOrdersAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var (rows, totalCount) = await _orderRepository.GetPagedAsync(
                pagination.Page, pagination.PageSize, pagination.Search, cancellationToken);

            return new PagedResponse<OrderListItemDto>
            {
                Success = true,
                Data = rows.Select(r => new OrderListItemDto
                {
                    OrderNo = r.OrderNo,
                    VendorName = r.VendorName,
                    OrderDate = r.OrderDate.ToString("yyyy-MM-dd"),
                    Handler = r.HandlerUserName ?? "",
                    HandlerName = r.HandlerFullName ?? r.HandlerUserName ?? "",
                    TotalAmount = r.TotalAmount,
                    Status = r.Status,
                    StatusText = OrderStatus.GetText(r.Status),
                    CancelReason = r.CancelReason
                }).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };
        }

        public async Task<SingleResponse<OrderDetailDto>> GetOrderDetailAsync(
            string orderNo, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByOrderNoWithDetailsAsync(orderNo, cancellationToken);

            if (order == null)
            {
                throw new NotFoundException("Không tìm thấy đơn hàng");
            }

            var lines = (order.OrderProducts ?? new List<OrderProduct>())
                .Select(op => new OrderProductLineDto
                {
                    ItemCode = op.ItemCode,
                    ItemName = op.ItemName,
                    Unit = op.Unit,
                    Quantity = op.Quantity,
                    UnitPrice = op.UnitPrice,
                    LineAmount = op.LineAmount
                }).ToList();

            var history = await _historyRepository.GetByOrderIdAsync(order.Id, cancellationToken);

            return new SingleResponse<OrderDetailDto>
            {
                Success = true,
                Data = new OrderDetailDto
                {
                    OrderNo = order.OrderNo,
                    VendorName = order.Vendor?.VendorName ?? "",
                    OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                    Handler = order.HandlerUser?.UserId ?? "",
                    HandlerName = order.HandlerUser?.FullName ?? order.HandlerUser?.UserId ?? "",
                    TotalAmount = lines.Sum(l => l.LineAmount),
                    Status = order.Status,
                    StatusText = OrderStatus.GetText(order.Status),
                    Note = order.Note ?? "",

                    CancelReason = order.CancelReason,
                    CancelledAt = ToIso(order.CancelledAt),
                    CancelledByName = order.CancelledByUser?.FullName ?? order.CancelledByUser?.UserId,

                    OrderProducts = lines,

                    StatusHistory = history.Select(h => new OrderStatusHistoryDto
                    {
                        FromStatus = h.FromStatus,
                        FromStatusText = h.FromStatus == null ? null : OrderStatus.GetText(h.FromStatus),
                        ToStatus = h.ToStatus,
                        ToStatusText = OrderStatus.GetText(h.ToStatus),
                        Reason = h.Reason,
                        ChangedByName = h.ChangedByUser?.FullName ?? h.ChangedByUser?.UserId ?? "(hệ thống)",
                        ChangedAt = ToIso(h.ChangedAt)!
                    }).ToList()
                }
            };
        }

        public async Task<ApiResponse> CreateOrderAsync(
            CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            var vendor = await ResolveVendorAsync(request.VendorName, null, cancellationToken);

            var orderDate = ParseOrderDate(request.OrderDate);
            var handlerUserId = await ResolveHandlerAsync(request.Handler, cancellationToken);

            var lines = await BuildOrderLinesAsync(request.OrderProducts, cancellationToken);

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var seq = await _orderRepository.GetNextOrderSequenceAsync(cancellationToken);

                var order = new Order
                {
                    OrderNo = "PO" + seq.ToString("D5"),
                    VendorId = vendor.Id,
                    OrderDate = orderDate,
                    HandlerUserId = handlerUserId,
                    Status = OrderStatus.Draft, 
                    Note = request.Note
                };

                await _orderRepository.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); 

                foreach (var line in lines)
                {
                    line.OrderId = order.Id;
                    await _orderRepository.AddOrderProductAsync(line, cancellationToken);
                }

                await AddHistoryAsync(order.Id, null, OrderStatus.Draft, null, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ApiResponse.Ok("Tạo đơn hàng thành công", new OrderNoDto { OrderNo = order.OrderNo });
            }
            catch (DataPersistenceException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new BusinessRuleException("Có lỗi khi tạo đơn hàng, dữ liệu đã được hoàn tác");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ApiResponse> UpdateOrderAsync(
            string orderNo, UpdateOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Không tìm thấy đơn hàng");
            }

            if (order.Status != OrderStatus.Draft)
            {
                throw new BusinessRuleException("Chỉ có thể chỉnh sửa đơn hàng ở trạng thái Bản nháp");
            }

            var vendor = await ResolveVendorAsync(request.VendorName, order.VendorId, cancellationToken);

            var orderDate = ParseOrderDate(request.OrderDate);
            var handlerUserId = await ResolveHandlerAsync(request.Handler, cancellationToken);
            var lines = await BuildOrderLinesAsync(request.OrderProducts, cancellationToken);

            order.VendorId = vendor.Id;
            order.OrderDate = orderDate;
            order.HandlerUserId = handlerUserId;
            order.Note = request.Note;

            var oldItems = await _orderRepository.GetOrderProductsByOrderIdAsync(order.Id, cancellationToken);
            _orderRepository.RemoveOrderProducts(oldItems);

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var line in lines)
                {
                    line.OrderId = order.Id;
                    await _orderRepository.AddOrderProductAsync(line, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DataPersistenceException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new BusinessRuleException("Có lỗi khi cập nhật đơn hàng, dữ liệu đã được hoàn tác");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return ApiResponse.Ok("Cập nhật đơn hàng thành công", new OrderNoDto { OrderNo = order.OrderNo });
        }

        public async Task<ApiResponse> DeleteOrderAsync(
            string orderNo, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Không tìm thấy đơn hàng");
            }

            if (order.Status == OrderStatus.Received)
            {
                throw new BusinessRuleException(
                    "Đơn đã nhập kho thì không xoá được nữa, vì hàng đã vào kho và đã ghi " +
                    "vào sổ cái tồn kho. Hãy dùng chức năng xuất kho hoặc điều chỉnh tồn.");
            }

            if (order.Status != OrderStatus.Draft)
            {
                throw new BusinessRuleException(
                    $"Đơn đang ở trạng thái {OrderStatus.GetText(order.Status)}, không xoá được. " +
                    "Đơn đã xác nhận thì phải HUỶ kèm lý do để còn thống kê được.");
            }

            _orderRepository.Remove(order);

            await _auditLogger.LogAsync(
                nameof(Order), order.Id.ToString(), AuditAction.Delete,
                before: new { order.OrderNo, order.Status },
                description: $"Xoá đơn nháp {order.OrderNo}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Đã xoá đơn hàng");
        }

        public async Task<ApiResponse> CancelOrderAsync(
            string orderNo, CancelOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Không tìm thấy đơn hàng");
            }

            var reason = request.Reason?.Trim() ?? "";
            if (reason.Length == 0)
            {
                throw new BusinessRuleException("Phải nhập lý do huỷ đơn");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new BusinessRuleException("Đơn hàng này đã bị huỷ trước đó");
            }

            if (order.Status == OrderStatus.Received)
            {
                throw new BusinessRuleException(
                    "Đơn đã nhập kho thì không huỷ được, vì hàng đã vào kho và đã ghi vào sổ cái. " +
                    "Hãy dùng bút toán đảo hoặc xuất kho để trả lại hàng trước.");
            }

            var now = DateTime.UtcNow;
            var fromStatus = order.Status;

            order.CancelledAt = now;
            order.CancelledBy = _currentUser.Id;
            order.CancelReason = reason;

            await ChangeStatusAsync(order, OrderStatus.Cancelled, reason, cancellationToken);

            await _auditLogger.LogAsync(
                nameof(Order), order.Id.ToString(), AuditAction.OrderCancel,
                before: new { Status = fromStatus },
                after: new { Status = OrderStatus.Cancelled, Reason = reason },
                description: $"Huỷ đơn {order.OrderNo}: {reason}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Đã huỷ đơn {order.OrderNo}");
        }

        public async Task<ApiResponse> ConfirmOrderAsync(
            string orderNo, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Không tìm thấy đơn hàng");
            }

            var lines = await _orderRepository.GetOrderProductsByOrderIdAsync(order.Id, cancellationToken);
            if (lines.Count == 0)
            {
                throw new BusinessRuleException("Không thể xác nhận đơn hàng không có sản phẩm");
            }

            await ChangeStatusAsync(order, OrderStatus.Confirmed, null, cancellationToken);

            await _auditLogger.LogAsync(
                nameof(Order), order.Id.ToString(), AuditAction.OrderConfirm,
                description: $"Xác nhận đơn {order.OrderNo}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Đã xác nhận đơn hàng");
        }

        public async Task<ApiResponse> ConfirmStockAsync(
            string orderNo, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Không tìm thấy đơn hàng");
            }

            if (order.Status != OrderStatus.Confirmed)
            {
                throw new BusinessRuleException(
                    $"Chỉ nhập kho được đơn ở trạng thái Đã xác nhận (đơn này đang: {OrderStatus.GetText(order.Status)})");
            }

            if (await _stockRepository.HasOrderTransactionAsync(order.Id, cancellationToken))
            {
                throw new BusinessRuleException("Đơn hàng này đã được nhập kho trước đó");
            }

            var orderProducts = await _orderRepository.GetOrderProductsByOrderIdAsync(order.Id, cancellationToken);
            if (orderProducts.Count == 0)
            {
                throw new BusinessRuleException("Đơn hàng không có sản phẩm để nhập kho");
            }

            var now = DateTime.UtcNow;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var op in orderProducts)
                {
                    var stock = await _stockRepository.GetByProductIdAsync(op.ProductId, cancellationToken);

                    if (stock == null)
                    {
                        stock = new Stock { ProductId = op.ProductId, Quantity = 0 };
                        await _stockRepository.AddAsync(stock, cancellationToken);
                    }

                    var balanceBefore = stock.Quantity;
                    stock.Quantity += op.Quantity;

                    await _stockRepository.AddTransactionAsync(new StockTransaction
                    {
                        ProductId = op.ProductId,
                        TransactionType = StockTransactionType.In,
                        Quantity = op.Quantity,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = stock.Quantity,
                        UnitPrice = op.UnitPrice,
                        RefType = StockRefType.Order,
                        RefId = order.Id,
                        TransactionDate = now,
                        CreatedBy = _currentUser.Id,
                        Note = $"Nhập kho theo đơn {order.OrderNo}"
                    }, cancellationToken);
                }

                await ChangeStatusAsync(order, OrderStatus.Received, null, cancellationToken);

                await _auditLogger.LogAsync(
                    nameof(Order), order.Id.ToString(), AuditAction.OrderReceive,
                    after: new { Lines = orderProducts.Count },
                    description: $"Nhập kho theo đơn {order.OrderNo} ({orderProducts.Count} mặt hàng)",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DataPersistenceException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new BusinessRuleException(
                    "Đơn hàng này vừa được người khác nhập kho xong. Hãy tải lại trang để xem trạng thái mới nhất.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return ApiResponse.Ok("Đã nhập kho thành công");
        }



        private async Task ChangeStatusAsync(
            Order order, string toStatus, string? reason, CancellationToken cancellationToken)
        {
            var fromStatus = order.Status;

            if (!OrderStatus.CanTransition(fromStatus, toStatus))
            {
                throw new BusinessRuleException(
                    $"Không chuyển được đơn từ trạng thái {OrderStatus.GetText(fromStatus)} " +
                    $"sang {OrderStatus.GetText(toStatus)}.");
            }

            order.Status = toStatus;

            await AddHistoryAsync(order.Id, fromStatus, toStatus, reason, cancellationToken);
        }

        private Task AddHistoryAsync(
            int orderId, string? fromStatus, string toStatus, string? reason,
            CancellationToken cancellationToken)
        {
            return _historyRepository.AddAsync(new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Reason = reason,
                ChangedBy = _currentUser.Id,
                ChangedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        private static DateOnly ParseOrderDate(string value)
        {
            if (!DateOnly.TryParse(value, out var date))
            {
                throw new BusinessRuleException("Ngày đặt hàng không hợp lệ (định dạng yyyy-MM-dd)");
            }
            return date;
        }

        private static string? ToIso(DateTime? value)
            => value == null
                ? null
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        private async Task<Vendor> ResolveVendorAsync(
            string? vendorName, int? currentVendorId, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetByVendorNameAsync(vendorName ?? "", cancellationToken);
            if (vendor == null)
            {
                throw new BusinessRuleException("Nhà cung cấp không tồn tại");
            }

            if (!vendor.IsActive && vendor.Id != currentVendorId)
            {
                throw new BusinessRuleException(
                    $"Nhà cung cấp '{vendor.VendorName}' đã ngừng hợp tác, không lập đơn mới được");
            }

            return vendor;
        }

        private async Task<int?> ResolveHandlerAsync(string? handler, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(handler))
            {
                return null;
            }

            var user = await _userRepository.GetByUserIdAsync(handler.Trim(), cancellationToken);
            if (user == null)
            {
                throw new BusinessRuleException($"Nhân viên phụ trách '{handler}' không tồn tại");
            }

            return user.Id;
        }

        private async Task<List<OrderProduct>> BuildOrderLinesAsync(
            List<OrderProductItem> items, CancellationToken cancellationToken)
        {
            if (items == null || items.Count == 0)
            {
                throw new BusinessRuleException("Đơn hàng phải có ít nhất 1 sản phẩm");
            }

            var duplicated = items
                .GroupBy(i => i.ItemCode?.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicated != null)
            {
                throw new BusinessRuleException($"Sản phẩm '{duplicated.Key}' bị lặp lại trong đơn hàng");
            }

            var lines = new List<OrderProduct>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemCode))
                {
                    throw new BusinessRuleException("Mã sản phẩm không được để trống");
                }

                var product = await _productRepository.GetByItemCodeAsync(item.ItemCode.Trim(), cancellationToken);

                if (product == null)
                {
                    throw new BusinessRuleException($"Sản phẩm '{item.ItemCode}' không tồn tại");
                }

                if (!product.IsActive)
                {
                    throw new BusinessRuleException($"Sản phẩm '{item.ItemCode}' đã ngừng kinh doanh");
                }

                if (item.Quantity <= 0)
                {
                    throw new BusinessRuleException($"Số lượng của '{item.ItemCode}' phải lớn hơn 0");
                }

                var unitPrice = item.UnitPrice ?? product.Price;
                if (unitPrice < 0)
                {
                    throw new BusinessRuleException($"Đơn giá của '{item.ItemCode}' không được âm");
                }

                lines.Add(new OrderProduct
                {
                    ProductId = product.Id,

                    ItemCode = product.ItemCode,
                    ItemName = product.ItemName,
                    Unit = product.Unit,

                    Quantity = item.Quantity,
                    UnitPrice = unitPrice
                });
            }

            return lines;
        }
    }
}
