using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IUserService
    {
        Task<PagedResponse<UserDto>> GetUsersAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default);

        Task<SingleResponse<UserDto>> GetUserAsync(
            int id, CancellationToken cancellationToken = default);

        Task<ApiResponse> CreateUserAsync(
            UserCreateRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> UpdateUserAsync(
            int id, UserUpdateRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> ResetPasswordAsync(
            int id, AdminResetPasswordRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse> ChangeOwnPasswordAsync(
            ChangePasswordRequest request, CancellationToken cancellationToken = default);
    }
}
