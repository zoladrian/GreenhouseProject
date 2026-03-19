using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.IntegrationTests.Persistence;

public sealed class SqliteWalIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly GreenhouseDbContext _db;

    public SqliteWalIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-wal-test-{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new GreenhouseDbContext(options);
    }

    [Fact]
    public async Task ApplyWalAsync_ShouldSetWalJournalMode_OnFileDatabase()
    {
        await _db.Database.EnsureCreatedAsync();
        await SqlitePragmas.ApplyWalAsync(_db);

        var mode = await SqlitePragmas.GetJournalModeAsync(_db);

        Assert.Equal("wal", mode?.ToLowerInvariant());
    }

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            if (File.Exists(_dbPath + "-shm")) File.Delete(_dbPath + "-shm");
            if (File.Exists(_dbPath + "-wal")) File.Delete(_dbPath + "-wal");
        }
        catch
        {
            // ignore cleanup races
        }
    }
}
