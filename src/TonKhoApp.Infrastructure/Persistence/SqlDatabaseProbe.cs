using Microsoft.EntityFrameworkCore;
using TonKhoApp.Application.Interfaces;

namespace TonKhoApp.Infrastructure.Persistence
{
    public sealed class SqlDatabaseProbe : IDatabaseProbe
    {
        private readonly AppDbContext _context;

        public SqlDatabaseProbe(AppDbContext context)
        {
            _context = context;
        }

        public Task PingAsync(CancellationToken cancellationToken = default)
            => _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
    }
}
