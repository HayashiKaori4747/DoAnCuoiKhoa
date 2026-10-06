namespace TonKhoApp.Application.Interfaces
{
    public interface ICurrentUser
    {
        int? Id { get; }            
        string? UserName { get; }   // Users.UserId (tên đăng nhập)

        IReadOnlyList<string> Roles { get; }
        string? IpAddress { get; }
        bool IsInRole(string roleCode);
    }
}
