using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IProductService
    {
        Task<PagedResponse<ProductDto>> GetAllProductsAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default);

        Task<SingleResponse<ProductDto>> GetProductDetailAsync(
            string itemCode, CancellationToken cancellationToken = default);

        Task<ApiResponse> CreateProductAsync(
            ProductRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> UpdateProductAsync(
            string itemCode, ProductRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteProductAsync(
            string itemCode, CancellationToken cancellationToken = default);
    }
}
