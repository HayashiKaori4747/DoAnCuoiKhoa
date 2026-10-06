using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public StockService(
            IStockRepository stockRepository,
            IProductRepository productRepository,
            IAuditLogger auditLogger,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
            _auditLogger = auditLogger;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<PagedResponse<StockDto>> GetAllStockAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var (rows, totalCount) = await _stockRepository.GetPagedAsync(
                pagination.Page, pagination.PageSize, pagination.Search, cancellationToken);

            return new PagedResponse<StockDto>
            {
                Success = true,
                Data = rows.Select(r => new StockDto
                {
                    ItemCd = r.ItemCode,
                    ItemName = r.ItemName,
                    Stock = r.Quantity,
                    Unit = r.Unit,
                    Price = r.Price,
                    StockValue = r.StockValue,
                    MinQuantity = r.MinQuantity,
                    BelowMinimum = r.BelowMinimum
                }).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };
        }

        public async Task<ApiResponse> IssueStockAsync(
            StockIssueRequest request, CancellationToken cancellationToken = default)
        {
            var product = await GetProductOrThrowAsync(request.ItemCode, cancellationToken);

            if (request.Quantity <= 0)
            {
                throw new BusinessRuleException("Số lượng xuất phải lớn hơn 0");
            }

            var stock = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

            if (stock == null || stock.Quantity < request.Quantity)
            {
                var available = stock?.Quantity ?? 0;
                throw new BusinessRuleException(
                    $"Không đủ tồn kho: hiện còn {available:0.###} {product.Unit}, yêu cầu xuất {request.Quantity:0.###}");
            }

            var balanceBefore = stock.Quantity;
            stock.Quantity -= request.Quantity;

            await _stockRepository.AddTransactionAsync(new StockTransaction
            {
                ProductId = product.Id,
                TransactionType = StockTransactionType.Out,
                Quantity = -request.Quantity, 
                BalanceBefore = balanceBefore,
                BalanceAfter = stock.Quantity,
                UnitPrice = product.Price,
                RefType = StockRefType.Manual,
                TransactionDate = DateTime.UtcNow,
                CreatedBy = _currentUser.Id,
                Note = request.Note
            }, cancellationToken);

            await _auditLogger.LogAsync(
                nameof(Stock), product.ItemCode, AuditAction.StockIssue,
                before: new { Quantity = balanceBefore },
                after: new { Quantity = stock.Quantity },
                description: $"Xuất kho {request.Quantity:0.###} {product.Unit} mặt hàng {product.ItemCode}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok(
                $"Đã xuất {request.Quantity:0.###} {product.Unit}, tồn còn {stock.Quantity:0.###}");
        }

        public async Task<ApiResponse> AdjustStockAsync(
            StockAdjustRequest request, CancellationToken cancellationToken = default)
        {
            var product = await GetProductOrThrowAsync(request.ItemCode, cancellationToken);

            if (request.ActualQuantity < 0)
            {
                throw new BusinessRuleException("Số lượng thực tế không được âm");
            }

            var stock = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);
            if (stock == null)
            {
                stock = new Stock { ProductId = product.Id, Quantity = 0 };
                await _stockRepository.AddAsync(stock, cancellationToken);
            }

            var balanceBefore = stock.Quantity;
            var delta = request.ActualQuantity - balanceBefore;
            if (delta == 0)
            {
                return ApiResponse.Ok("Số lượng thực tế trùng với tồn kho, không cần điều chỉnh");
            }

            stock.Quantity = request.ActualQuantity;

            await _stockRepository.AddTransactionAsync(new StockTransaction
            {
                ProductId = product.Id,
                TransactionType = StockTransactionType.Adjust,
                Quantity = delta, 
                BalanceBefore = balanceBefore,
                BalanceAfter = stock.Quantity,
                UnitPrice = product.Price,
                RefType = StockRefType.Manual,
                TransactionDate = DateTime.UtcNow,
                CreatedBy = _currentUser.Id,
                Note = request.Note ?? "Điều chỉnh theo kiểm kê"
            }, cancellationToken);

            await _auditLogger.LogAsync(
                nameof(Stock), product.ItemCode, AuditAction.StockAdjust,
                before: new { Quantity = balanceBefore },
                after: new { Quantity = stock.Quantity, Delta = delta },
                description: $"Điều chỉnh tồn {product.ItemCode}: {balanceBefore:0.###} → {stock.Quantity:0.###} " +
                             $"({(delta > 0 ? "+" : "")}{delta:0.###}). {request.Note ?? "Theo kiểm kê"}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok(
                $"Đã điều chỉnh {(delta > 0 ? "+" : "")}{delta:0.###}, tồn hiện tại {stock.Quantity:0.###}");
        }

        public async Task<ApiResponse> ReverseTransactionAsync(
            StockReversalRequest request, CancellationToken cancellationToken = default)
        {
            var original = await _stockRepository.GetTransactionByIdAsync(
                request.TransactionId, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy bút toán #{request.TransactionId}");

            if (original.TransactionType == StockTransactionType.Reversal)
            {
                throw new BusinessRuleException(
                    "Không đảo được một bút toán đảo. Nếu bút toán đảo này sai, " +
                    "hãy dùng chức năng điều chỉnh tồn theo kiểm kê.");
            }

            if (await _stockRepository.HasReversalAsync(original.Id, cancellationToken))
            {
                throw new BusinessRuleException($"Bút toán #{original.Id} đã được đảo trước đó rồi");
            }

            var stock = await _stockRepository.GetByProductIdAsync(original.ProductId, cancellationToken)
                ?? throw new BusinessRuleException("Không tìm thấy dòng tồn kho của mặt hàng này");

            var reversalQuantity = -original.Quantity;
            var balanceBefore = stock.Quantity;
            var balanceAfter = balanceBefore + reversalQuantity;

            if (balanceAfter < 0)
            {
                throw new BusinessRuleException(
                    $"Không đảo được bút toán này: tồn hiện tại {balanceBefore:0.###} không đủ để " +
                    $"trừ {Math.Abs(reversalQuantity):0.###}. Hàng đã được xuất đi sau bút toán đó. " +
                    "Hãy dùng chức năng điều chỉnh tồn theo kiểm kê.");
            }

            var product = await _productRepository.GetByIdAsync(original.ProductId, cancellationToken);
            var itemCode = product?.ItemCode ?? original.ProductId.ToString();

            stock.Quantity = balanceAfter;

            await _stockRepository.AddTransactionAsync(new StockTransaction
            {
                ProductId = original.ProductId,
                TransactionType = StockTransactionType.Reversal,
                Quantity = reversalQuantity,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                UnitPrice = original.UnitPrice,

                RefType = StockRefType.Reversal,
                RefId = null,

                OriginalTransactionId = original.Id,
                TransactionDate = DateTime.UtcNow,
                CreatedBy = _currentUser.Id,
                Note = $"Đảo bút toán #{original.Id}: {request.Reason.Trim()}"
            }, cancellationToken);

            await _auditLogger.LogAsync(
                nameof(StockTransaction), original.Id.ToString(), AuditAction.StockReversal,
                before: new { original.TransactionType, original.Quantity, original.BalanceAfter },
                after: new { Quantity = reversalQuantity, BalanceAfter = balanceAfter },
                description: $"Đảo bút toán #{original.Id} của {itemCode}: {request.Reason.Trim()}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok(
                $"Đã ghi bút toán đảo cho #{original.Id}. Tồn kho {itemCode}: " +
                $"{balanceBefore:0.###} → {balanceAfter:0.###}");
        }

        public async Task<PagedResponse<StockTransactionDto>> GetTransactionsAsync(
            string itemCode, PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var product = await GetProductOrThrowAsync(itemCode, cancellationToken);

            var (items, totalCount) = await _stockRepository.GetTransactionsAsync(
                product.Id, pagination.Page, pagination.PageSize, cancellationToken);

            return new PagedResponse<StockTransactionDto>
            {
                Success = true,
                Data = items.Select(t => new StockTransactionDto
                {
                    Id = t.Id,
                    ItemCode = product.ItemCode,
                    TransactionType = t.TransactionType,
                    TransactionTypeText = StockTransactionType.GetText(t.TransactionType),
                    Quantity = t.Quantity,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    UnitPrice = t.UnitPrice,
                    RefType = t.RefType,
                    RefId = t.RefId,
                    RefNo = t.RefNo,
                    OriginalTransactionId = t.OriginalTransactionId,
                    TransactionDate = DateTime.SpecifyKind(t.TransactionDate, DateTimeKind.Utc)
                        .ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    CreatedByName = t.CreatedByName ?? "",
                    Note = t.Note
                }).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };
        }

        private async Task<Product> GetProductOrThrowAsync(string itemCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new BusinessRuleException("Mã sản phẩm không được để trống");
            }

            var product = await _productRepository.GetByItemCodeAsync(itemCode.Trim(), cancellationToken);
            if (product == null)
            {
                throw new NotFoundException($"Không tìm thấy sản phẩm '{itemCode}'");
            }

            return product;
        }
    }
}
