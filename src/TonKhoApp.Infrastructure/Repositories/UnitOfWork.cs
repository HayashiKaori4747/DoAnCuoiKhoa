using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Exceptions;
using TonKhoApp.Infrastructure.Persistence;

namespace TonKhoApp.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException(
                    "Dữ liệu vừa bị người khác thay đổi, vui lòng tải lại và thử lại.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new DataPersistenceException(ex.Message, ex);
            }
        }

        public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new EfTransaction(tx);
        }

        private sealed class EfTransaction : IAppTransaction
        {
            private readonly IDbContextTransaction _tx;

            public EfTransaction(IDbContextTransaction tx) => _tx = tx;

            public Task CommitAsync(CancellationToken cancellationToken = default)
                => _tx.CommitAsync(cancellationToken);

            public Task RollbackAsync(CancellationToken cancellationToken = default)
                => _tx.RollbackAsync(cancellationToken);

            public ValueTask DisposeAsync() => _tx.DisposeAsync();
        }
    }
}
