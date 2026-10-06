using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(
            LoginRequest request, CancellationToken cancellationToken = default);

        Task<LoginResult> RefreshAsync(
            string refreshToken, CancellationToken cancellationToken = default);

        Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

        Task<ApiResponse> GetAllUsersAsync(CancellationToken cancellationToken = default);
    }
}
