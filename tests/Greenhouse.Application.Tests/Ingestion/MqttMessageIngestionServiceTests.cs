using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Tests.Ingestion;

public sealed class MqttMessageIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ShouldPersistReading_ForSensorTopic()
    {
        var parser = new FakeParser(new ParsedSensorPayload(10m, 21.2m, 99, 200));
        var repository = new InMemoryReadingRepository();
        var sut = new MqttMessageIngestionService(parser, repository);

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Czujnik wilgotnosci 1",
            "{\"soil_moisture\":10,\"temperature\":21.2,\"battery\":99,\"linkquality\":200}",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal("Czujnik wilgotnosci 1", repository.Items[0].SensorIdentifier);
    }

    [Fact]
    public async Task IngestAsync_ShouldIgnoreBridgeTopic()
    {
        var parser = new FakeParser(new ParsedSensorPayload(null, null, null, null));
        var repository = new InMemoryReadingRepository();
        var sut = new MqttMessageIngestionService(parser, repository);

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/bridge/health",
            "{\"status\":\"ok\"}",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        Assert.Empty(repository.Items);
    }

    private sealed class FakeParser : IMqttPayloadParser
    {
        private readonly ParsedSensorPayload _result;

        public FakeParser(ParsedSensorPayload result)
        {
            _result = result;
        }

        public ParsedSensorPayload ParseSensorPayload(string payloadJson) => _result;
    }

    private sealed class InMemoryReadingRepository : ISensorReadingRepository
    {
        public List<SensorReading> Items { get; } = [];

        public Task AddAsync(SensorReading reading, CancellationToken cancellationToken)
        {
            Items.Add(reading);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<SensorReading>)Items.Take(count).ToList());
    }
}
