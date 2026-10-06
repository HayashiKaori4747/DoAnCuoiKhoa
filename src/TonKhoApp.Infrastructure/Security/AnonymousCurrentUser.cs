using TonKhoApp.Application.Interfaces;

namespace TonKhoApp.Infrastructure.Security
{
    public class AnonymousCurrentUser : ICurrentUser
    {
        public int? Id => null;
        public string? UserName => null;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public string? IpAddress => null;
        public bool IsInRole(string roleCode) => false;
    }
}
