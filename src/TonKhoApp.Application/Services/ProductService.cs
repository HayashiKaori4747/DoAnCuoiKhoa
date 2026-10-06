using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IStockRepository stockRepository,
            IAuditLogger auditLogger,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _stockRepository = stockRepository;
            _auditLogger = auditLogger;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<ProductDto>> GetAllProductsAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _productRepository.GetPagedAsync(
                pagination.Page, pagination.PageSize, pagination.Search,
                pagination.ActiveOnly ?? false, cancellationToken);

            return new PagedResponse<ProductDto>
            {
                Success = true,
                Data = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };
        }

        public async Task<SingleResponse<ProductDto>> GetProductDetailAsync(
            string itemCode, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByItemCodeAsync(itemCode, cancellationToken);

            if (product == null)
            {
                throw new NotFoundException("Không tìm thấy sản phẩm");
            }

            return new SingleResponse<ProductDto> { Success = true, Data = ToDto(product) };
        }

        public async Task<ApiResponse> CreateProductAsync(
            ProductRequest request, CancellationToken cancellationToken = default)
        {
            var itemCode = request.ItemCode?.Trim() ?? "";
            if (string.IsNullOrEmpty(itemCode))
            {
                throw new BusinessRuleException("Mã sản phẩm không được để trống");
            }

            if (await _productRepository.ExistsByItemCodeAsync(itemCode, cancellationToken))
            {
                throw new BusinessRuleException("Mã sản phẩm đã tồn tại");
            }

            var product = new Product
            {
                ItemCode = itemCode,
                ItemName = request.ItemName?.Trim() ?? "",
                Unit = string.IsNullOrWhiteSpace(request.Unit) ? "kg" : request.Unit.Trim(),
                Price = request.Price ?? 0,   
                MinQuantity = request.MinQuantity ?? 0,
                IsActive = request.IsActive ?? true
            };

            await _productRepository.AddAsync(product, cancellationToken);

            try
            {
                await _auditLogger.LogAsync(
                    nameof(Product), itemCode, AuditAction.Create,
                    after: new { product.ItemCode, product.ItemName, product.Unit, product.Price, product.MinQuantity },
                    description: $"Tạo sản phẩm {itemCode}",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DataPersistenceException)
            {
                throw new BusinessRuleException("Mã sản phẩm đã tồn tại");
            }

            return ApiResponse.Ok("Tạo sản phẩm thành công");
        }

        public async Task<ApiResponse> UpdateProductAsync(
            string itemCode, ProductRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByItemCodeAsync(itemCode, cancellationToken);

            if (product == null)
            {
                throw new NotFoundException("Không tìm thấy sản phẩm");
            }

            var before = new
            {
                product.ItemName, product.Unit, product.Price, product.MinQuantity, product.IsActive
            };
            var oldPrice = product.Price;

            product.ItemName = request.ItemName?.Trim() ?? product.ItemName;
            product.Unit = string.IsNullOrWhiteSpace(request.Unit) ? product.Unit : request.Unit.Trim();
            product.Price = request.Price ?? product.Price;
            product.MinQuantity = request.MinQuantity ?? product.MinQuantity;
            product.IsActive = request.IsActive ?? product.IsActive;

            var after = new
            {
                product.ItemName, product.Unit, product.Price, product.MinQuantity, product.IsActive
            };

            await _auditLogger.LogAsync(
                nameof(Product), product.ItemCode, AuditAction.Update, before, after,
                $"Cập nhật sản phẩm {product.ItemCode}", cancellationToken: cancellationToken);

            if (oldPrice != product.Price)
            {
                await _auditLogger.LogAsync(
                    nameof(Product), product.ItemCode, AuditAction.PriceChange,
                    before: new { Price = oldPrice },
                    after: new { product.Price },
                    description: $"Sửa giá {product.ItemCode}: {oldPrice:N2} → {product.Price:N2}",
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Cập nhật sản phẩm thành công");
        }

        // Xoá sản phẩm
        public async Task<ApiResponse> DeleteProductAsync(
            string itemCode, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByItemCodeAsync(itemCode, cancellationToken);

            if (product == null)
            {
                throw new NotFoundException("Không tìm thấy sản phẩm");
            }

            var usedInOrders = await _orderRepository.IsProductUsedInOrdersAsync(product.Id, cancellationToken);
            var hasLedger = await _stockRepository.HasTransactionsAsync(product.Id, cancellationToken);

            if (usedInOrders || hasLedger)
            {
                if (!product.IsActive)
                {
                    throw new BusinessRuleException("Sản phẩm này đã ở trạng thái ngừng kinh doanh");
                }

                product.IsActive = false;

                await _auditLogger.LogAsync(
                    nameof(Product), product.ItemCode, AuditAction.Update,
                    before: new { IsActive = true }, after: new { IsActive = false },
                    description: $"Ngừng kinh doanh {product.ItemCode} (đã phát sinh đơn hàng/tồn kho nên không xoá)",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return ApiResponse.Ok(
                    "Sản phẩm đã phát sinh đơn hàng/tồn kho nên được chuyển sang trạng thái Ngừng kinh doanh thay vì xoá");
            }

            var stock = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);
            if (stock != null)
            {
                _stockRepository.Remove(stock);
            }

            _productRepository.Remove(product);

            await _auditLogger.LogAsync(
                nameof(Product), product.ItemCode, AuditAction.Delete,
                before: new { product.ItemCode, product.ItemName, product.Price },
                description: $"Xoá sản phẩm {product.ItemCode}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Xoá sản phẩm thành công");
        }

        private static ProductDto ToDto(Product p) => new()
        {
            ItemCode = p.ItemCode,
            ItemName = p.ItemName,
            Unit = p.Unit,
            Price = p.Price,
            MinQuantity = p.MinQuantity,
            IsActive = p.IsActive
        };
    }
}
