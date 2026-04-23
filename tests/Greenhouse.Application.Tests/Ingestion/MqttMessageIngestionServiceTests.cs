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
        var provisioning = new FixedProvisioning(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var sut = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Czujnik wilgotnosci 1",
            "{\"soil_moisture\":10,\"temperature\":21.2,\"battery\":99,\"linkquality\":200}",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal("Czujnik wilgotnosci 1", repository.Items[0].SensorIdentifier);
    }

    [Fact]
    public async Task IngestAsync_ShouldPersistNormalizedIeee_AsSensorIdentifier_WhenPresentInPayload()
    {
        const string ieee = "0x00158d0001a2b3c4";
        var parser = new FakeParser(new ParsedSensorPayload(10m, 21.2m, 99, 200, ieee));
        var repository = new InMemoryReadingRepository();
        var provisioning = new FixedProvisioning(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var sut = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Alocasia",
            $"{{\"ieee_address\":\"{ieee}\",\"soil_moisture\":10,\"temperature\":21.2,\"battery\":99,\"linkquality\":200}}",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(ieee, repository.Items[0].SensorIdentifier);
    }

    [Fact]
    public async Task IngestAsync_ShouldIgnoreAvailabilitySubtopic()
    {
        var parser = new FakeParser(new ParsedSensorPayload(null, null, null, null));
        var repository = new InMemoryReadingRepository();
        var provisioning = new FixedProvisioning(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var sut = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/czujnik1/availability",
            "online",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task IngestAsync_ShouldIgnoreBridgeTopic()
    {
        var parser = new FakeParser(new ParsedSensorPayload(null, null, null, null));
        var repository = new InMemoryReadingRepository();
        var provisioning = new FixedProvisioning(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var sut = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/bridge/health",
            "{\"status\":\"ok\"}",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task IngestAsync_ShouldPersistWeatherMetrics_ForRainSensorPayload()
    {
        var parser = new FakeParser(new ParsedSensorPayload(
            SoilMoisture: null,
            Temperature: null,
            Battery: 87,
            LinkQuality: 190,
            IeeeAddress: "0x00158d0001a2b3c4",
            Rain: true,
            RainIntensityRaw: 11m,
            IlluminanceRaw: 470m,
            IlluminanceAverage20MinRaw: 390m,
            IlluminanceMaximumTodayRaw: 980m,
            CleaningReminder: false));
        var repository = new InMemoryReadingRepository();
        var provisioning = new FixedProvisioning(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        var sut = new MqttMessageIngestionService(
            parser,
            repository,
            provisioning,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MqttMessageIngestionService>.Instance,
            new MqttIngestTelemetry());

        var message = new IncomingMqttMessage(
            "zigbee2mqtt/Deszcz_1",
            "{\"rain\":true,\"rain_intensity\":11,\"battery\":87}",
            DateTime.UtcNow);

        await sut.IngestAsync(message, CancellationToken.None);

        var reading = Assert.Single(repository.Items);
        Assert.True(reading.Rain);
        Assert.Equal(11m, reading.RainIntensityRaw);
        Assert.Equal(470m, reading.IlluminanceRaw);
        Assert.Equal(390m, reading.IlluminanceAverage20MinRaw);
        Assert.Equal(980m, reading.IlluminanceMaximumTodayRaw);
        Assert.False(reading.CleaningReminder);
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

    private sealed class FixedProvisioning : ISensorProvisioningService
    {
        private readonly Guid _id;

        public FixedProvisioning(Guid id)
        {
            _id = id;
        }

        public Task<SensorEnsureResult> EnsureSensorAsync(EnsureSensorInput input, CancellationToken cancellationToken) =>
            Task.FromResult(new SensorEnsureResult(_id, CreatedNew: false));
    }

    private sealed class CapturingProvisioning : ISensorProvisioningService
    {
        public string? LastExternalId { get; private set; }
        private static readonly Guid Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public Task<SensorEnsureResult> EnsureSensorAsync(EnsureSensorInput input, CancellationToken cancellationToken)
        {
            LastExternalId = input.CanonicalExternalId;
            return Task.FromResult(new SensorEnsureResult(Id, CreatedNew: false));
        }
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

        public Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(
            IReadOnlyList<Guid> sensorIds, DateTime from, DateTime to, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<SensorReading>)Items
                .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value)
                            && r.ReceivedAtUtc >= from && r.ReceivedAtUtc <= to)
                .ToList());

        public Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(
            IReadOnlyList<Guid> sensorIds, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<SensorReading>)Items
                .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value))
                .GroupBy(r => r.SensorId)
                .Select(g => g.OrderByDescending(r => r.ReceivedAtUtc).First())
                .ToList());

        public Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task<int> AlignSensorIdentifierForSensorAsync(Guid sensorId, string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<bool> ExistsDuplicateAsync(string sensorIdentifier, DateTime receivedAtUtc, string topic, string rawPayloadJson, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(x =>
                x.SensorIdentifier == sensorIdentifier &&
                x.Topic == topic &&
                x.RawPayloadJson == rawPayloadJson &&
                Math.Abs((x.ReceivedAtUtc - receivedAtUtc).TotalSeconds) <= 2));
    }
}
