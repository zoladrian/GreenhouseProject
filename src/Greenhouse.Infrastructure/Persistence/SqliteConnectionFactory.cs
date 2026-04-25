using Microsoft.Data.Sqlite;

namespace Greenhouse.Infrastructure.Persistence;

/// <summary>
/// Buduje connection string SQLite z bezpiecznymi defaultami pod warunki Maliny:
/// Cache=Shared umożliwia wielu klientom (API + Worker + cleanup) korzystanie ze wspólnego cache,
/// Pooling=true redukuje koszt otwierania połączeń, Mode=ReadWriteCreate jest wprost.
/// PRAGMA-y (WAL, synchronous, busy_timeout) wstrzykuje <see cref="SqlitePragmaConnectionInterceptor"/>
/// po otwarciu połączenia.
/// </summary>
public static class SqliteConnectionFactory
{
    public static string BuildConnectionString(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 30,
        };
        return builder.ToString();
    }
}
