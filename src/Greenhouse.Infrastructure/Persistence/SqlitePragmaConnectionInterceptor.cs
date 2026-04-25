using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Greenhouse.Infrastructure.Persistence;

/// <summary>
/// Aplikuje PRAGMA-y SQLite na każdym świeżo otwartym połączeniu:
/// - <c>journal_mode=WAL</c> — write-ahead log; równoległe odczyty nie blokują pojedynczego pisarza.
/// - <c>synchronous=NORMAL</c> — bezpieczny kompromis dla maliny z szybkim SD; FULL jest tu nadmiarowy.
/// - <c>busy_timeout=5000</c> — zamiast natychmiastowego SQLITE_BUSY przy konflikcie poczekaj 5 s.
/// - <c>foreign_keys=ON</c> — wymuszenie FK; SQLite domyślnie ich nie egzekwuje.
/// - <c>temp_store=MEMORY</c> — sortowania/temp w RAM zamiast dziennikiem na SD.
///
/// Idempotentne — można odpalać przy każdym OpenAsync. WAL ustawia się raz na bazę,
/// reszta jest per-połączenie.
/// </summary>
public sealed class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
{
    private const string PragmaScript = """
        PRAGMA journal_mode=WAL;
        PRAGMA synchronous=NORMAL;
        PRAGMA busy_timeout=5000;
        PRAGMA foreign_keys=ON;
        PRAGMA temp_store=MEMORY;
        """;

    private readonly ILogger<SqlitePragmaConnectionInterceptor>? _logger;

    public SqlitePragmaConnectionInterceptor(ILogger<SqlitePragmaConnectionInterceptor>? logger = null)
    {
        _logger = logger;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        return Task.CompletedTask;
    }

    private void ApplyPragmas(DbConnection connection)
    {
        if (connection is not SqliteConnection sqlite)
        {
            return;
        }

        try
        {
            using var cmd = sqlite.CreateCommand();
            cmd.CommandText = PragmaScript;
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            // PRAGMA-y są idempotentne, ale przy bardzo równoległym otwieraniu może wpaść SQLITE_BUSY
            // na journal_mode=WAL. Logujemy warn — kolejne OpenAsync je dociągnie.
            _logger?.LogWarning(ex, "Nie udało się ustawić wszystkich PRAGMA SQLite (kontynuuję z domyślnymi).");
        }
    }
}
