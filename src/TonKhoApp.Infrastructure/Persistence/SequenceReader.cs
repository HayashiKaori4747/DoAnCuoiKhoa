using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TonKhoApp.Infrastructure.Persistence
{
    internal static class SequenceReader
    {
        public static async Task<long> NextValueAsync(
            DbContext context, string sequenceName, CancellationToken cancellationToken = default)
        {
            var connection = context.Database.GetDbConnection();
            var mustClose = connection.State != System.Data.ConnectionState.Open;

            if (mustClose)
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT NEXT VALUE FOR dbo.{sequenceName}";

                var currentTransaction = context.Database.CurrentTransaction;
                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt64(result);
            }
            finally
            {
                if (mustClose)
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }
    }
}
