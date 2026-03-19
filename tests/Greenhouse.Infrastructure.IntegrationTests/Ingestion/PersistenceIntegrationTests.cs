using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
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

        ISensorReadingRepository repository = new EfSensorReadingRepository(dbContext);
        IMqttPayloadParser parser = new JsonMqttPayloadParser();
        var service = new MqttMessageIngestionService(parser, repository);

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Czujnik wilgotnosci 2",
            "{\"battery\":100,\"linkquality\":255,\"soil_moisture\":0,\"temperature\":23.8}",
            DateTime.UtcNow);

        await service.IngestAsync(message, CancellationToken.None);
        var latest = await repository.GetLatestAsync(10, CancellationToken.None);

        Assert.Single(latest);
        Assert.Equal(23.8m, latest[0].Temperature);
        Assert.Equal(0m, latest[0].SoilMoisture);
    }
}
