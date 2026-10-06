using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IVendorService
    {
        Task<PagedResponse<VendorDto>> GetAllVendorsAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default);

        Task<SingleResponse<VendorDto>> GetVendorDetailAsync(
            int id, CancellationToken cancellationToken = default);

        Task<ApiResponse> CreateVendorAsync(
            VendorRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> UpdateVendorAsync(
            int id, VendorRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteVendorAsync(
            int id, CancellationToken cancellationToken = default);
    }
}
