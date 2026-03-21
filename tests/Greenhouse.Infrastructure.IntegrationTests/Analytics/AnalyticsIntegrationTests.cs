using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Sensors;
using Greenhouse.Domain.Nawy;
using Greenhouse.Infrastructure.Mqtt;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.IntegrationTests.Analytics;

public sealed class AnalyticsIntegrationTests : IDisposable
{
    private readonly GreenhouseDbContext _db;

    public AnalyticsIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new GreenhouseDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task WateringEvents_ShouldDetectSpike()
    {
        var (nawaId, _) = await SeedNawaWithSensorAndReadings(
            ("czujnik-wet", new[]
            {
                (DateTime.UtcNow.AddMinutes(-60), 20m),
                (DateTime.UtcNow.AddMinutes(-55), 22m),
                (DateTime.UtcNow.AddMinutes(-50), 48m),
                (DateTime.UtcNow.AddMinutes(-40), 46m)
            }));

        _db.ChangeTracker.Clear();

        var sensorRepo = new EfSensorRepository(_db);
        var readingRepo = new EfSensorReadingRepository(_db, new JsonMqttPayloadParser());

        var service = new GetWateringEventsQueryService(sensorRepo, readingRepo);
        var events = await service.ExecuteAsync(
            nawaId,
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.Single(events);
        Assert.Equal(26m, events[0].DeltaMoisture);
        Assert.Equal("likelyManual", events[0].InferredKind);
        Assert.Equal(1, events[0].ContributingSensorCount);
    }

    [Fact]
    public async Task WateringEvents_TwoSensorsInWindow_ShouldInferLikelyRain()
    {
        var t0 = DateTime.UtcNow.AddMinutes(-90);
        var nawaId = await SeedNawaWithTwoSensorsWateringSpikes(
            ("s-a", new[]
            {
                (t0, 20m),
                (t0.AddMinutes(5), 45m),
            }),
            ("s-b", new[]
            {
                (t0.AddMinutes(8), 22m),
                (t0.AddMinutes(12), 48m),
            }));

        _db.ChangeTracker.Clear();

        var sensorRepo = new EfSensorRepository(_db);
        var readingRepo = new EfSensorReadingRepository(_db, new JsonMqttPayloadParser());
        var service = new GetWateringEventsQueryService(sensorRepo, readingRepo);
        var events = await service.ExecuteAsync(
            nawaId,
            DateTime.UtcNow.AddHours(-3),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.Single(events);
        Assert.Equal("likelyRain", events[0].InferredKind);
        Assert.Equal(2, events[0].ContributingSensorCount);
    }

    [Fact]
    public async Task DryingRate_ShouldEstimate()
    {
        var (nawaId, _) = await SeedNawaWithSensorAndReadings(
            ("czujnik-dry", new[]
            {
                (DateTime.UtcNow.AddHours(-3), 60m),
                (DateTime.UtcNow.AddHours(-2), 55m),
                (DateTime.UtcNow.AddHours(-1), 50m)
            }));

        _db.ChangeTracker.Clear();

        var sensorRepo = new EfSensorRepository(_db);
        var readingRepo = new EfSensorReadingRepository(_db, new JsonMqttPayloadParser());

        var service = new GetDryingRatesQueryService(sensorRepo, readingRepo);
        var rates = await service.ExecuteAsync(
            nawaId,
            DateTime.UtcNow.AddHours(-4),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.Single(rates);
        Assert.True(rates[0].PercentPerHour > 0);
    }

    [Fact]
    public async Task MoistureSeries_ShouldReturnChronological()
    {
        var (nawaId, _) = await SeedNawaWithSensorAndReadings(
            ("czujnik-series", new[]
            {
                (DateTime.UtcNow.AddMinutes(-30), 30m),
                (DateTime.UtcNow.AddMinutes(-20), 35m),
                (DateTime.UtcNow.AddMinutes(-10), 32m)
            }));

        _db.ChangeTracker.Clear();

        var sensorRepo = new EfSensorRepository(_db);
        var readingRepo = new EfSensorReadingRepository(_db, new JsonMqttPayloadParser());

        var service = new GetMoistureSeriesQueryService(sensorRepo, readingRepo);
        var points = await service.ExecuteAsync(
            nawaId, null,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.Equal(3, points.Count);
        Assert.True(points[0].UtcTime < points[1].UtcTime);
        Assert.True(points[1].UtcTime < points[2].UtcTime);
    }

    private async Task<(Guid NawaId, Guid SensorId)> SeedNawaWithSensorAndReadings(
        (string externalId, (DateTime time, decimal moisture)[] readings) sensorData)
    {
        var nawaRepo = new EfNawaRepository(_db);
        var sensorRepo = new EfSensorRepository(_db);
        var parser = new JsonMqttPayloadParser();
        var readingRepo = new EfSensorReadingRepository(_db, parser);

        var nawa = Nawa.Create($"Nawa-{Guid.NewGuid():N}", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var provisioning = new SensorProvisioningService(sensorRepo, readingRepo);
        var ingestion = new MqttMessageIngestionService(
            parser,
            readingRepo,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        foreach (var (time, moisture) in sensorData.readings)
        {
            await ingestion.IngestAsync(new IncomingMqttMessage(
                $"zigbee2mqtt/{sensorData.externalId}",
                $"{{\"soil_moisture\":{moisture},\"temperature\":22,\"battery\":90,\"linkquality\":100}}",
                time), CancellationToken.None);
        }

        _db.ChangeTracker.Clear();
        var untracked = await sensorRepo.GetByExternalIdAsync(sensorData.externalId, CancellationToken.None);
        var tracked = await sensorRepo.GetByIdAsync(untracked!.Id, CancellationToken.None);
        tracked!.AssignToNawa(nawa.Id);
        await sensorRepo.SaveChangesAsync(CancellationToken.None);

        return (nawa.Id, tracked.Id);
    }

    private async Task<Guid> SeedNawaWithTwoSensorsWateringSpikes(
        (string externalId, (DateTime time, decimal moisture)[] readings) sensorA,
        (string externalId, (DateTime time, decimal moisture)[] readings) sensorB)
    {
        var nawaRepo = new EfNawaRepository(_db);
        var sensorRepo = new EfSensorRepository(_db);
        var parser = new JsonMqttPayloadParser();
        var readingRepo = new EfSensorReadingRepository(_db, parser);

        var nawa = Nawa.Create($"Nawa-{Guid.NewGuid():N}", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var provisioning = new SensorProvisioningService(sensorRepo, readingRepo);
        var ingestion = new MqttMessageIngestionService(
            parser,
            readingRepo,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        foreach (var sensorData in new[] { sensorA, sensorB })
        {
            foreach (var (time, moisture) in sensorData.readings)
            {
                await ingestion.IngestAsync(new IncomingMqttMessage(
                    $"zigbee2mqtt/{sensorData.externalId}",
                    $"{{\"soil_moisture\":{moisture},\"temperature\":22,\"battery\":90,\"linkquality\":100}}",
                    time), CancellationToken.None);
            }
        }

        _db.ChangeTracker.Clear();

        foreach (var ext in new[] { sensorA.externalId, sensorB.externalId })
        {
            var untracked = await sensorRepo.GetByExternalIdAsync(ext, CancellationToken.None);
            var tracked = await sensorRepo.GetByIdAsync(untracked!.Id, CancellationToken.None);
            tracked!.AssignToNawa(nawa.Id);
            await sensorRepo.SaveChangesAsync(CancellationToken.None);
        }

        return nawa.Id;
    }

    public void Dispose() => _db.Dispose();
}
