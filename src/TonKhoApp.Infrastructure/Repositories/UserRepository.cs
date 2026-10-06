using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUserIdAsync(
            string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles!).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.FindAsync(new object?[] { id }, cancellationToken);
        }

        public async Task<User?> GetByIdWithRolesAsync(
            int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles!).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByUserIdAsync(
            string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.UserId == userId, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLike(search.Trim())}%";
                query = query.Where(u =>
                    EF.Functions.Like(u.UserId, pattern) ||
                    (u.FullName != null && EF.Functions.Like(u.FullName, pattern)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(u => u.IsActive) 
                .ThenBy(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(u => u.UserRoles!).ThenInclude(ur => ur.Role)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IReadOnlyList<string>> GetRoleCodesAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role!.Code)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.Role!.Code == RoleCode.Admin)
                .Join(_context.Users.Where(u => u.IsActive),
                      ur => ur.UserId, u => u.Id, (ur, u) => u.Id)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users.CountAsync(cancellationToken);
        }

        private static string EscapeLike(string value)
            => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }

        public async Task AddUserRoleAsync(
            UserRole userRole, CancellationToken cancellationToken = default)
        {
            await _context.UserRoles.AddAsync(userRole, cancellationToken);
        }

        public void RemoveUserRoles(IEnumerable<UserRole> userRoles)
        {
            _context.UserRoles.RemoveRange(userRoles);
        }
    }
}
