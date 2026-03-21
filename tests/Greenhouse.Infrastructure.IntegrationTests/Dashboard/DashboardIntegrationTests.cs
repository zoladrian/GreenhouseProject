using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Sensors;
using Greenhouse.Application.Voice;
using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.Sensors;
using Greenhouse.Infrastructure.Mqtt;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Greenhouse.Infrastructure.IntegrationTests.Dashboard;

public sealed class DashboardIntegrationTests : IDisposable
{
    private readonly GreenhouseDbContext _db;

    public DashboardIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new GreenhouseDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task FullPipeline_IngestThenDashboard()
    {
        var nawaRepo = new EfNawaRepository(_db);
        var sensorRepo = new EfSensorRepository(_db);
        var parser = new JsonMqttPayloadParser();
        var readingRepo = new EfSensorReadingRepository(_db, parser);

        var nawa = Nawa.Create("Nawa integration", null);
        nawa.UpdateMoistureThresholds(10m, 90m);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var provisioning = new SensorProvisioningService(sensorRepo, readingRepo);
        var ingestion = new MqttMessageIngestionService(
            parser,
            readingRepo,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        await ingestion.IngestAsync(new IncomingMqttMessage(
            "zigbee2mqtt/czujnik1",
            "{\"soil_moisture\":45,\"temperature\":22.5,\"battery\":85,\"linkquality\":120}",
            DateTime.UtcNow.AddMinutes(-2)), CancellationToken.None);

        _db.ChangeTracker.Clear();

        var sensorId = (await sensorRepo.GetByExternalIdAsync("czujnik1", CancellationToken.None))!.Id;
        var sensor = await sensorRepo.GetByIdAsync(sensorId, CancellationToken.None);
        sensor!.AssignToNawa(nawa.Id);
        await sensorRepo.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var voice = Options.Create(new VoiceOptions());
        var watering = new GetWateringEventsQueryService(sensorRepo, readingRepo);
        var dashboardService = new GetDashboardQueryService(
            nawaRepo,
            sensorRepo,
            readingRepo,
            NullLogger<GetDashboardQueryService>.Instance,
            voice,
            watering);
        var snapshots = await dashboardService.ExecuteAsync(CancellationToken.None);

        Assert.Single(snapshots);
        var snap = snapshots[0];
        Assert.Equal("Nawa integration", snap.NawaName);
        Assert.Equal(1, snap.SensorCount);
        Assert.Equal(45m, snap.AvgMoisture);
        Assert.Equal(85, snap.LowestBattery);
        Assert.Equal(OperatorStatus.Ok, snap.Status);
    }

    [Fact]
    public async Task SensorHealth_ReturnsData()
    {
        var sensorRepo = new EfSensorRepository(_db);
        var parser = new JsonMqttPayloadParser();
        var readingRepo = new EfSensorReadingRepository(_db, parser);
        var provisioning = new SensorProvisioningService(sensorRepo, readingRepo);
        var ingestion = new MqttMessageIngestionService(
            parser,
            readingRepo,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        await ingestion.IngestAsync(new IncomingMqttMessage(
            "zigbee2mqtt/sensor-health-test",
            "{\"soil_moisture\":30,\"temperature\":20,\"battery\":50,\"linkquality\":80}",
            DateTime.UtcNow), CancellationToken.None);

        _db.ChangeTracker.Clear();

        var healthService = new GetSensorHealthQueryService(sensorRepo, readingRepo);
        var result = await healthService.ExecuteAsync(CancellationToken.None);

        Assert.Contains(result, h => h.ExternalId == "sensor-health-test" && h.Battery == 50);
    }

    public void Dispose() => _db.Dispose();
}
