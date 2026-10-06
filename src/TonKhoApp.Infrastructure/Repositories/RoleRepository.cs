using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .AsNoTracking()
                .OrderByDescending(r => r.IsSystem)  
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Roles.FindAsync(new object?[] { id }, cancellationToken);
        }

        public async Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetByCodesAsync(
            IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var list = codes.Distinct(StringComparer.Ordinal).ToList();

            return await _context.Roles
                .Where(r => list.Contains(r.Code))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Roles.AnyAsync(r => r.Code == code, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken);
        }

        public async Task<int> CountUsersInRoleAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Join(_context.Users, ur => ur.UserId, u => u.Id, (ur, u) => u.Id)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        {
            await _context.Roles.AddAsync(role, cancellationToken);
        }

        public void Remove(Role role)
        {
            _context.Roles.Remove(role);
        }
    }
}
