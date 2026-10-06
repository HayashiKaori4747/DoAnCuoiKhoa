using System.Security.Claims;
using TonKhoApp.Application.Interfaces;

namespace TonKhoApp.WebApi.Services
{
    public class CurrentUserService : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext? Context => _httpContextAccessor.HttpContext;

        private ClaimsPrincipal? User => Context?.User;

        public int? Id
        {
            get
            {
                var raw = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(raw, out var id) ? id : null;
            }
        }

        public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;

        public IReadOnlyList<string> Roles =>
            User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            ?? (IReadOnlyList<string>)Array.Empty<string>();

        public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

        public bool IsInRole(string roleCode) => User?.IsInRole(roleCode) ?? false;
    }
}
