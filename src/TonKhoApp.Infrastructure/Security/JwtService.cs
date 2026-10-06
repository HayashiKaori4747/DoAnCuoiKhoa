using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Security
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private const int DefaultAccessTokenMinutes = 15;
        private const int DefaultRefreshTokenDays = 7;

        public int AccessTokenMinutes => ReadPositiveInt("ExpirationMinutes", DefaultAccessTokenMinutes);
        public int RefreshTokenDays => ReadPositiveInt("RefreshTokenDays", DefaultRefreshTokenDays);

        private int ReadPositiveInt(string key, int fallback)
        {
            var raw = _configuration.GetSection("JwtSettings")[key];
            return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
        }

        public string GenerateAccessToken(User user, IEnumerable<string> roleCodes)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = _configuration["JWTSECRETKEY"] ?? jwtSettings["SecretKey"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID user
                new(ClaimTypes.Name, user.UserId),                  // Tên đăng nhập
                new(ClaimTypes.GivenName, user.FullName ?? ""),     // Họ tên

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var code in roleCodes.Distinct(StringComparer.Ordinal))
            {
                claims.Add(new Claim(ClaimTypes.Role, code));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string Token, string TokenHash) GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes);

            return (token, HashRefreshToken(token));
        }

        public string HashRefreshToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }
    }
}
