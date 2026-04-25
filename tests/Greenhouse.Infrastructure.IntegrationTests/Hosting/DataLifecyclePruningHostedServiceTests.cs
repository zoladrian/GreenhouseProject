using Greenhouse.Domain.SensorReadings;
using Greenhouse.Infrastructure.Hosting;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Greenhouse.Infrastructure.IntegrationTests.Hosting;

/// <summary>
/// Sprawdza, że hosted service pruningu rzeczywiście usuwa wpisy starsze niż KeepReadingsDays
/// i NIE usuwa świeżych. To krytyczne dla malinki, gdzie SQLite na karcie SD ma ograniczone IO
/// — nieskończenie rosnąca tabela = degradacja zapisu po miesiącach.
/// </summary>
public sealed class DataLifecyclePruningHostedServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ServiceProvider _sp;

    public DataLifecyclePruningHostedServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-prune-test-{Guid.NewGuid()}.db");
        var services = new ServiceCollection();
        services.AddDbContext<GreenhouseDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteOldReadings_AndKeepFresh()
    {
        // Arrange: 3 stare (>200 dni), 2 świeże (1 dzień).
        using (var seed = _sp.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
            var now = DateTime.UtcNow;
            db.SensorReadings.Add(SensorReading.Create("s-old-1", now.AddDays(-220), "t/old", "{}", 50m, 20m, 90, 100));
            db.SensorReadings.Add(SensorReading.Create("s-old-2", now.AddDays(-201), "t/old", "{}", 51m, 21m, 90, 100));
            db.SensorReadings.Add(SensorReading.Create("s-old-3", now.AddDays(-181), "t/old", "{}", 52m, 22m, 90, 100));
            db.SensorReadings.Add(SensorReading.Create("s-fresh-1", now.AddDays(-1), "t/fresh", "{}", 60m, 23m, 95, 110));
            db.SensorReadings.Add(SensorReading.Create("s-fresh-2", now.AddHours(-2), "t/fresh", "{}", 61m, 23m, 95, 110));
            await db.SaveChangesAsync();
        }

        var svc = new DataLifecyclePruningHostedService(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DataLifecyclePruningHostedService>.Instance,
            Options.Create(new DataLifecycleOptions
            {
                EnablePruning = true,
                KeepReadingsDays = 180,
                PruneIntervalHours = 1,
            }));

        using var cts = new CancellationTokenSource();
        await svc.StartAsync(cts.Token);
        // Pierwszy przebieg jest natychmiastowy; daj mu chwilę.
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await cts.CancelAsync();
        await svc.StopAsync(CancellationToken.None);

        using var assertScope = _sp.CreateScope();
        var dbCheck = assertScope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
        var remaining = await dbCheck.SensorReadings.Select(r => r.SensorIdentifier).ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.All(remaining, id => Assert.StartsWith("s-fresh-", id));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotPrune_WhenDisabled()
    {
        using (var seed = _sp.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
            db.SensorReadings.Add(SensorReading.Create("s-old", DateTime.UtcNow.AddDays(-500), "t", "{}", 50m, 20m, 90, 100));
            await db.SaveChangesAsync();
        }

        var svc = new DataLifecyclePruningHostedService(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DataLifecyclePruningHostedService>.Instance,
            Options.Create(new DataLifecycleOptions { EnablePruning = false }));

        using var cts = new CancellationTokenSource();
        await svc.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        await cts.CancelAsync();
        await svc.StopAsync(CancellationToken.None);

        using var assertScope = _sp.CreateScope();
        var dbCheck = assertScope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
        Assert.Equal(1, await dbCheck.SensorReadings.CountAsync());
    }

    public void Dispose()
    {
        _sp.Dispose();
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
