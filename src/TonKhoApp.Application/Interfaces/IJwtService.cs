using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user, IEnumerable<string> roleCodes);

        (string Token, string TokenHash) GenerateRefreshToken();
        string HashRefreshToken(string token);
        int AccessTokenMinutes { get; }
        int RefreshTokenDays { get; }
    }
}
