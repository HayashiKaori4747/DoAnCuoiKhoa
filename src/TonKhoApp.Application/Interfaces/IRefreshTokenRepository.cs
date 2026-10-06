using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
       
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        
        Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
            int userId, DateTime nowUtc, CancellationToken cancellationToken = default);

        Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
        Task<int> RevokeAllForUserAsync(
            int userId, string reason, DateTime nowUtc, CancellationToken cancellationToken = default);
        Task<int> DeleteExpiredBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    }
}
