using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Interfaces
{
    public interface IRoleRepository
    {
        Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Role>> GetByCodesAsync(
            IEnumerable<string> codes, CancellationToken cancellationToken = default);

        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<int> CountUsersInRoleAsync(int roleId, CancellationToken cancellationToken = default);

        Task AddAsync(Role role, CancellationToken cancellationToken = default);

        void Remove(Role role);
    }
}
