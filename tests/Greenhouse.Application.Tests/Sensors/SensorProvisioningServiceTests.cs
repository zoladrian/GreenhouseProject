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
    }
}
