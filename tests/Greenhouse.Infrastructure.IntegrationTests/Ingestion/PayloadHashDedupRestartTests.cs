using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Sensors;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Infrastructure.Mqtt;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.IntegrationTests.Ingestion;

/// <summary>
/// Symuluje scenariusz „retained MQTT po restarcie API”: ten sam payload przychodzi
/// drugi raz z brokera, bo Mosquitto/Z2M wysyła ostatni stan przy reconnect.
/// Stara dedup-policy (equality całego rawPayload + window 2 s) bywała ślepa,
/// gdy timestamp lokalny przesunął się minimalnie. Hash + window jest deterministyczny.
/// </summary>
public sealed class PayloadHashDedupRestartTests
{
    private static (GreenhouseDbContext db, ISensorReadingRepository repo) NewDb(SqliteConnection conn)
    {
        var dbOptions = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new GreenhouseDbContext(dbOptions);
        db.Database.EnsureCreated();
        var parser = new JsonMqttPayloadParser();
        return (db, new EfSensorReadingRepository(db, parser));
    }

    [Fact]
    public void ComputePayloadHash_IsDeterministic_ForSameTopicAndPayload()
    {
        var h1 = SensorReading.ComputePayloadHash("zigbee2mqtt/x", "{\"a\":1}");
        var h2 = SensorReading.ComputePayloadHash("zigbee2mqtt/x", "{\"a\":1}");
        var hOther = SensorReading.ComputePayloadHash("zigbee2mqtt/x", "{\"a\":2}");
        var hOtherTopic = SensorReading.ComputePayloadHash("zigbee2mqtt/y", "{\"a\":1}");

        Assert.Equal(h1, h2);
        Assert.NotEqual(h1, hOther);
        Assert.NotEqual(h1, hOtherTopic);
        Assert.Equal(64, h1.Length);
    }

    [Fact]
    public async Task RestartReplay_DoesNotDoubleInsert_WhenSamePayloadArrivesWithinWindow()
    {
        await using var conn = new SqliteConnection("Data Source=:memory:");
        await conn.OpenAsync();

        var (db1, repo1) = NewDb(conn);
        var sensorRepo1 = new EfSensorRepository(db1);
        var provisioning1 = new SensorProvisioningService(sensorRepo1, repo1);
        var ingest1 = new MqttMessageIngestionService(
            new JsonMqttPayloadParser(), repo1, provisioning1,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var at = DateTime.UtcNow;
        var payload = "{\"soil_moisture\":42,\"battery\":91,\"linkquality\":150}";
        await ingest1.IngestAsync(
            new IncomingMqttMessage("zigbee2mqtt/restart-test", payload, at),
            CancellationToken.None);

        // „Restart”: nowy DbContext na tej samej bazie SQLite.
        await db1.DisposeAsync();
        var (db2, repo2) = NewDb(conn);
        var sensorRepo2 = new EfSensorRepository(db2);
        var provisioning2 = new SensorProvisioningService(sensorRepo2, repo2);
        var ingest2 = new MqttMessageIngestionService(
            new JsonMqttPayloadParser(), repo2, provisioning2,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        // Replay tego samego payloadu z minimalnie przesuniętym timestampem (broker często odtwarza w ms-okolicy).
        await ingest2.IngestAsync(
            new IncomingMqttMessage("zigbee2mqtt/restart-test", payload, at.AddMilliseconds(800)),
            CancellationToken.None);

        var latest = await repo2.GetLatestAsync(10, CancellationToken.None);
        Assert.Single(latest);
        Assert.Equal(64, latest[0].PayloadHash?.Length);

        await db2.DisposeAsync();
    }

    [Fact]
    public async Task SamePayload_OutsideWindow_DoesPersistAgain()
    {
        await using var conn = new SqliteConnection("Data Source=:memory:");
        await conn.OpenAsync();

        var (db, repo) = NewDb(conn);
        var sensorRepo = new EfSensorRepository(db);
        var provisioning = new SensorProvisioningService(sensorRepo, repo);
        var ingest = new MqttMessageIngestionService(
            new JsonMqttPayloadParser(), repo, provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var at = DateTime.UtcNow.AddMinutes(-10);
        var payload = "{\"soil_moisture\":42,\"battery\":91,\"linkquality\":150}";
        await ingest.IngestAsync(new IncomingMqttMessage("zigbee2mqtt/window-test", payload, at), CancellationToken.None);
        // Ten sam payload, ale 5 minut później — to nowa próbka, musi być zapisana.
        await ingest.IngestAsync(new IncomingMqttMessage("zigbee2mqtt/window-test", payload, at.AddMinutes(5)), CancellationToken.None);

        var latest = await repo.GetLatestAsync(10, CancellationToken.None);
        Assert.Equal(2, latest.Count);

        await db.DisposeAsync();
    }
}
