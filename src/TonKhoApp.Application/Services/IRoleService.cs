using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IRoleService
    {
        Task<ApiResponse> GetRolesAsync(CancellationToken cancellationToken = default);

        Task<ApiResponse> CreateRoleAsync(
            RoleRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> UpdateRoleAsync(
            int id, RoleRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);
    }
}
