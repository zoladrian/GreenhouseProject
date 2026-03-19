using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

/// <summary>
/// Ustawienia PRAGMA dla SQLite — lepsza współbieżność odczytów (WAL).
/// </summary>
public static class SqlitePragmas
{
    /// <summary>
    /// Wywołać po utworzeniu bazy (EnsureCreated / migracje).
    /// </summary>
    public static async Task ApplyWalAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }

    /// <summary>
    /// Do testów / diagnostyki.
    /// </summary>
    public static async Task<string?> GetJournalModeAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode;";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result?.ToString();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
