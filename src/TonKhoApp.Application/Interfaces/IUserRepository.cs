using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<User?> GetByIdWithRolesAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetAllActiveAsync(CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetRoleCodesAsync(
            int userId, CancellationToken cancellationToken = default);

        Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
        void RemoveUserRoles(IEnumerable<UserRole> userRoles);
    }
}
