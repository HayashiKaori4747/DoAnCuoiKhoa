using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByHashAsync(
            string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Include(t => t.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
            int userId, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > nowUtc)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(token, cancellationToken);
        }

        public async Task<int> RevokeAllForUserAsync(
            int userId, string reason, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(t => t.RevokedAt, nowUtc)
                          .SetProperty(t => t.RevokedReason, reason),
                    cancellationToken);
        }

        public async Task<int> DeleteExpiredBeforeAsync(
            DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(t => t.ExpiresAt < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
