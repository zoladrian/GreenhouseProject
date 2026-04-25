using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Sensors;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Tests.Sensors;

public sealed class SensorProvisioningServiceTests
{
    [Fact]
    public async Task EnsureSensor_ShouldRekey_WhenIeeeMatchesLegacyFriendlyRow()
    {
        const string ieee = "0x00158d0001a2b3c4";
        var repo = new SensorListRepo();
        var readings = new ReadingRepo();
        var legacy = Sensor.Register("old_name");
        repo.Items.Add(legacy);
        readings.AddForSensor(legacy.Id, $"{{\"ieee_address\":\"{ieee}\"}}");

        var sut = new SensorProvisioningService(repo, readings);
        var result = await sut.EnsureSensorAsync(
            new EnsureSensorInput("new_name", ieee),
            CancellationToken.None);

        Assert.False(result.CreatedNew);
        Assert.Equal(legacy.Id, result.SensorId);
        Assert.Equal("0x00158d0001a2b3c4", legacy.ExternalId);
        Assert.Equal("new_name", legacy.DisplayName);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task EnsureSensor_ShouldCreateNewSensor_WhenNeitherCanonicalNorFriendlyMatch()
    {
        var repo = new SensorListRepo();
        var readings = new ReadingRepo();

        var sut = new SensorProvisioningService(repo, readings);
        var result = await sut.EnsureSensorAsync(
            new EnsureSensorInput("brand_new_topic", "brand_new_topic"),
            CancellationToken.None);

        Assert.True(result.CreatedNew);
        Assert.Single(repo.Items);
        var s = repo.Items[0];
        Assert.Equal("brand_new_topic", s.ExternalId);
        Assert.Equal("brand_new_topic", s.DisplayName);
    }

    [Fact]
    public async Task EnsureSensor_ShouldUpdateDisplayName_WhenCanonicalAlreadyExists()
    {
        // Z2M zmieniło friendly_name dla tego samego IEEE → DisplayName w bazie ma podążać.
        const string ieee = "0x00158d0001abcdef";
        var existing = Sensor.Register(ieee);
        existing.SetDisplayName("old_friendly");
        var repo = new SensorListRepo();
        repo.Items.Add(existing);

        var sut = new SensorProvisioningService(repo, new ReadingRepo());
        var result = await sut.EnsureSensorAsync(
            new EnsureSensorInput("new_friendly", ieee),
            CancellationToken.None);

        Assert.False(result.CreatedNew);
        Assert.Equal(existing.Id, result.SensorId);
        Assert.Equal("new_friendly", existing.DisplayName);
        Assert.Equal(ieee, existing.ExternalId);
    }

    [Fact]
    public async Task EnsureSensor_ShouldRekey_WhenByFriendlyExistsAndCanonicalIsIeee()
    {
        // Stary wpis trzymał klucz po friendly_name; teraz Z2M zaczął publikować ieee_address.
        const string ieee = "0x00158d000ffffeee";
        var repo = new SensorListRepo();
        var readings = new ReadingRepo();
        var existingByFriendly = Sensor.Register("salon_glebowy");
        repo.Items.Add(existingByFriendly);

        var sut = new SensorProvisioningService(repo, readings);
        var result = await sut.EnsureSensorAsync(
            new EnsureSensorInput("salon_glebowy", ieee),
            CancellationToken.None);

        Assert.False(result.CreatedNew);
        Assert.Equal(existingByFriendly.Id, result.SensorId);
        Assert.Equal(ieee, existingByFriendly.ExternalId);
        Assert.Equal("salon_glebowy", existingByFriendly.DisplayName);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task EnsureSensor_ShouldUpdateKindHint_WhenProvided()
    {
        var existing = Sensor.Register("rain_outdoor");
        var repo = new SensorListRepo();
        repo.Items.Add(existing);

        var sut = new SensorProvisioningService(repo, new ReadingRepo());
        await sut.EnsureSensorAsync(
            new EnsureSensorInput("rain_outdoor", "rain_outdoor", SensorKind.Weather),
            CancellationToken.None);

        Assert.Equal(SensorKind.Weather, existing.Kind);
    }

    [Fact]
    public async Task EnsureSensor_ShouldNotMatchByFriendly_WhenNoExistingRowAndIeeeMismatch()
    {
        // Friendly_name conflict scenario: NIE robimy fałszywego dopasowania, jeśli IEEE
        // nie zgadza się z żadnym poprzednim odczytem (czyli nie należy do tego urządzenia).
        const string ieee = "0x00158d0099999999";
        var repo = new SensorListRepo();
        var readings = new ReadingRepo();
        var unrelated = Sensor.Register("inny");
        repo.Items.Add(unrelated);
        readings.AddForSensor(unrelated.Id, "{\"ieee_address\":\"0x00158d0011112222\"}");

        var sut = new SensorProvisioningService(repo, readings);
        var result = await sut.EnsureSensorAsync(
            new EnsureSensorInput("nowy_topic", ieee),
            CancellationToken.None);

        Assert.True(result.CreatedNew);
        Assert.Equal(2, repo.Items.Count);
        Assert.Contains(repo.Items, s => s.ExternalId == ieee);
    }

    private sealed class SensorListRepo : ISensorRepository
    {
        public List<Sensor> Items { get; } = [];

        public Task AddAsync(Sensor sensor, CancellationToken cancellationToken)
        {
            Items.Add(sensor);
            return Task.CompletedTask;
        }

        public Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

        public Task<Sensor?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(s => s.ExternalId == externalId));

        public Task<Sensor?> GetByExternalIdForUpdateAsync(string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(s => s.ExternalId == externalId));

        public Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Sensor>)Items.ToList());

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ReadingRepo : ISensorReadingRepository
    {
        private readonly Dictionary<Guid, string> _latestJson = [];

        public void AddForSensor(Guid sensorId, string payloadJson) => _latestJson[sensorId] = payloadJson;

        public Task AddAsync(SensorReading reading, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(
            IReadOnlyList<Guid> sensorIds, DateTime from, DateTime to, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(
            IReadOnlyList<Guid> sensorIds, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken cancellationToken)
        {
            if (!_latestJson.TryGetValue(sensorId, out var json))
                return Task.FromResult<string?>(null);

            if (json.Contains("00158d0001a2b3c4", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<string?>("0x00158d0001a2b3c4");

            return Task.FromResult<string?>(null);
        }

        public Task<int> AlignSensorIdentifierForSensorAsync(Guid sensorId, string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<bool> ExistsDuplicateAsync(string sensorIdentifier, DateTime receivedAtUtc, string topic, string rawPayloadJson, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
