using Greenhouse.Domain.SensorReadings;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Sprawdza, że PRAGMA-y SQLite faktycznie się aplikują (WAL, busy_timeout, foreign_keys)
/// oraz że równoległe zapisy z dwóch DbContextów nie wywalają SQLITE_BUSY.
/// Integracja używa pliku tymczasowego — :memory: nie pokazuje WAL i nie symuluje rzeczywistego konfliktu plików.
/// </summary>
public sealed class SqlitePragmaIntegrationTests : IDisposable
{
    private readonly string _dbPath;

    public SqlitePragmaIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"gh-pragma-test-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        TryDelete(_dbPath);
        TryDelete(_dbPath + "-wal");
        TryDelete(_dbPath + "-shm");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { /* WAL/SHM mogą trzymać locki — testowy katalog tmp poradzi sobie sam */ }
    }

    private GreenhouseDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(SqliteConnectionFactory.BuildConnectionString(_dbPath))
            .AddInterceptors(new SqlitePragmaConnectionInterceptor())
            .Options;
        var ctx = new GreenhouseDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task PragmaInterceptor_AppliesWal_BusyTimeout_AndForeignKeys()
    {
        await using var ctx = CreateContext();
        await ctx.Database.OpenConnectionAsync();
        var conn = (SqliteConnection)ctx.Database.GetDbConnection();

        Assert.Equal("wal", await ScalarAsync(conn, "PRAGMA journal_mode;"));
        // SQLite zwraca synchronous jako int: NORMAL = 1.
        Assert.Equal("1", await ScalarAsync(conn, "PRAGMA synchronous;"));
        Assert.Equal("5000", await ScalarAsync(conn, "PRAGMA busy_timeout;"));
        Assert.Equal("1", await ScalarAsync(conn, "PRAGMA foreign_keys;"));
    }

    [Fact]
    public void ConnectionString_Uses_SharedCache_AndPooling()
    {
        var cs = SqliteConnectionFactory.BuildConnectionString(_dbPath);
        var b = new SqliteConnectionStringBuilder(cs);
        Assert.Equal(SqliteCacheMode.Shared, b.Cache);
        Assert.True(b.Pooling);
        Assert.Equal(SqliteOpenMode.ReadWriteCreate, b.Mode);
    }

    [Fact]
    public async Task ParallelWriters_DoNotThrowSqliteBusy_WhenWalAndBusyTimeoutEnabled()
    {
        // Inicjalizacja schematu jednym kontekstem (i ustawienie WAL).
        await using (var seed = CreateContext())
        {
            await seed.SaveChangesAsync();
        }

        const int writers = 5;
        const int rowsPerWriter = 20;
        var errors = new List<Exception>();
        var errorsLock = new object();

        var tasks = Enumerable.Range(0, writers).Select(async writerIdx =>
        {
            try
            {
                for (var i = 0; i < rowsPerWriter; i++)
                {
                    await using var ctx = CreateContext();
                    var reading = SensorReading.Create(
                        sensorIdentifier: $"sensor-{writerIdx}",
                        receivedAtUtc: DateTime.UtcNow.AddMilliseconds(writerIdx * 1000 + i),
                        topic: $"zigbee2mqtt/sensor-{writerIdx}",
                        rawPayloadJson: $"{{\"i\":{i}}}",
                        soilMoisture: 42m,
                        temperature: 22m,
                        battery: 90,
                        linkQuality: 150);
                    ctx.SensorReadings.Add(reading);
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                lock (errorsLock) errors.Add(ex);
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Empty(errors);

        await using var verify = CreateContext();
        var total = await verify.SensorReadings.CountAsync();
        Assert.Equal(writers * rowsPerWriter, total);
    }

    private static async Task<string> ScalarAsync(SqliteConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString()?.ToLowerInvariant() ?? string.Empty;
    }
}
