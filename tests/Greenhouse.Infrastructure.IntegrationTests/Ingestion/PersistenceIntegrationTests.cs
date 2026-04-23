using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Sensors;
using Greenhouse.Infrastructure.Mqtt;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.IntegrationTests.Ingestion;

public sealed class PersistenceIntegrationTests
{
    [Fact]
    public async Task IngestAsync_ShouldPersistReadingToSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new GreenhouseDbContext(dbOptions);
        await dbContext.Database.EnsureCreatedAsync();

        IMqttPayloadParser parser = new JsonMqttPayloadParser();
        ISensorReadingRepository repository = new EfSensorReadingRepository(dbContext, parser);
        ISensorRepository sensorRepository = new EfSensorRepository(dbContext);
        var provisioning = new SensorProvisioningService(sensorRepository, repository);
        var service = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Czujnik wilgotnosci 2",
            "{\"battery\":100,\"linkquality\":255,\"soil_moisture\":0,\"temperature\":23.8}",
            DateTime.UtcNow);

        await service.IngestAsync(message, CancellationToken.None);
        var latest = await repository.GetLatestAsync(10, CancellationToken.None);

        Assert.Single(latest);
        Assert.Equal(23.8m, latest[0].Temperature);
        Assert.Equal(0m, latest[0].SoilMoisture);
        Assert.NotNull(latest[0].SensorId);
    }

    [Fact]
    public async Task IngestAsync_AfterZ2mFriendlyRename_ShouldKeepSingleSensor_ByIeeeInPayload()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new GreenhouseDbContext(dbOptions);
        await dbContext.Database.EnsureCreatedAsync();

        IMqttPayloadParser parser = new JsonMqttPayloadParser();
        ISensorReadingRepository repository = new EfSensorReadingRepository(dbContext, parser);
        ISensorRepository sensorRepository = new EfSensorRepository(dbContext);
        var provisioning = new SensorProvisioningService(sensorRepository, repository);
        var service = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        const string ieee = "0x00158d0001a2b3c4";
        // Najpierw wpis bez IEEE (stary klient) — ExternalId = nazwa z topicu
        var m0 = new IncomingMqttMessage(
            "zigbee2mqtt/stary_nickname",
            "{\"soil_moisture\":39}",
            DateTime.UtcNow.AddMinutes(-5));
        await service.IngestAsync(m0, CancellationToken.None);

        // Ten sam topic, już z IEEE w JSON (skan ostatnich odczytów znajdzie IEEE przy rename)
        var m0b = new IncomingMqttMessage(
            "zigbee2mqtt/stary_nickname",
            $"{{\"soil_moisture\":40,\"ieee_address\":\"{ieee}\"}}",
            DateTime.UtcNow.AddMinutes(-4));
        await service.IngestAsync(m0b, CancellationToken.None);

        var m2 = new IncomingMqttMessage(
            "zigbee2mqtt/nowy_nickname",
            $"{{\"soil_moisture\":41,\"ieee_address\":\"{ieee}\"}}",
            DateTime.UtcNow.AddMinutes(-3));
        await service.IngestAsync(m2, CancellationToken.None);

        var sensors = await sensorRepository.ListAsync(CancellationToken.None);
        Assert.Single(sensors);
        Assert.Equal(ieee, sensors[0].ExternalId, ignoreCase: true);
        Assert.Equal("nowy_nickname", sensors[0].DisplayName);

        var latest = await repository.GetLatestAsync(10, CancellationToken.None);
        Assert.All(latest, r => Assert.Equal(sensors[0].Id, r.SensorId));
    }

    [Fact]
    public async Task IngestAsync_ShouldPersistWeatherFieldsToSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new GreenhouseDbContext(dbOptions);
        await dbContext.Database.EnsureCreatedAsync();

        IMqttPayloadParser parser = new JsonMqttPayloadParser();
        ISensorReadingRepository repository = new EfSensorReadingRepository(dbContext, parser);
        ISensorRepository sensorRepository = new EfSensorRepository(dbContext);
        var provisioning = new SensorProvisioningService(sensorRepository, repository);
        var service = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Deszcz_1",
            "{\"rain\":true,\"rain_intensity\":15,\"illuminance_raw\":515,\"illuminance_average_20min\":490,\"illuminance_maximum_today\":910,\"cleaning_reminder\":false}",
            DateTime.UtcNow);

        await service.IngestAsync(message, CancellationToken.None);
        var latest = await repository.GetLatestAsync(1, CancellationToken.None);

        var row = Assert.Single(latest);
        Assert.True(row.Rain);
        Assert.Equal(15m, row.RainIntensityRaw);
        Assert.Equal(515m, row.IlluminanceRaw);
        Assert.Equal(490m, row.IlluminanceAverage20MinRaw);
        Assert.Equal(910m, row.IlluminanceMaximumTodayRaw);
        Assert.False(row.CleaningReminder);
    }

    [Fact]
    public async Task IngestAsync_ShouldSkipDuplicatePayload_InShortWindow()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new GreenhouseDbContext(dbOptions);
        await dbContext.Database.EnsureCreatedAsync();

        IMqttPayloadParser parser = new JsonMqttPayloadParser();
        ISensorReadingRepository repository = new EfSensorReadingRepository(dbContext, parser);
        ISensorRepository sensorRepository = new EfSensorRepository(dbContext);
        var provisioning = new SensorProvisioningService(sensorRepository, repository);
        var service = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var at = DateTime.UtcNow;
        var payload = "{\"soil_moisture\":38,\"temperature\":22.1}";
        await service.IngestAsync(new IncomingMqttMessage("zigbee2mqtt/dupe", payload, at), CancellationToken.None);
        await service.IngestAsync(new IncomingMqttMessage("zigbee2mqtt/dupe", payload, at.AddSeconds(1)), CancellationToken.None);

        var latest = await repository.GetLatestAsync(10, CancellationToken.None);
        Assert.Single(latest);
    }
}
