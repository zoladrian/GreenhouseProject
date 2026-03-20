using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Nawy;
using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Domain.Sensors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greenhouse.Application.Tests.Dashboard;

public sealed class GetDashboardQueryServiceTests
{
    [Fact]
    public async Task Dashboard_ShouldReturnNoData_WhenNoSensors()
    {
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Nawa A", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var sut = new GetDashboardQueryService(nawaRepo, sensorRepo, readingRepo, NullLogger<GetDashboardQueryService>.Instance);
        var result = await sut.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(OperatorStatus.NoData, result[0].Status);
        Assert.Equal(0, result[0].SensorCount);
        Assert.Equal(0, result[0].MoistureReadingCount);
    }

    [Fact]
    public async Task Dashboard_ShouldCalculateAverages()
    {
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Nawa B", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var sensor1 = Sensor.Register("sensor-1");
        sensor1.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(sensor1, CancellationToken.None);

        var sensor2 = Sensor.Register("sensor-2");
        sensor2.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(sensor2, CancellationToken.None);

        readingRepo.Add(SensorReading.Create("sensor-1", DateTime.UtcNow.AddMinutes(-5), "t", "{}", 40m, 22m, 95, 100, sensor1.Id));
        readingRepo.Add(SensorReading.Create("sensor-2", DateTime.UtcNow.AddMinutes(-3), "t", "{}", 60m, 24m, 80, 120, sensor2.Id));

        var sut = new GetDashboardQueryService(nawaRepo, sensorRepo, readingRepo, NullLogger<GetDashboardQueryService>.Instance);
        var result = await sut.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        var snapshot = result[0];
        Assert.Equal(2, snapshot.SensorCount);
        Assert.Equal(2, snapshot.MoistureReadingCount);
        Assert.Equal(50m, snapshot.AvgMoisture);
        Assert.Equal(40m, snapshot.MinMoisture);
        Assert.Equal(60m, snapshot.MaxMoisture);
        Assert.Equal(20m, snapshot.MoistureSpread);
        Assert.Equal(80, snapshot.LowestBattery);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnConflict_WhenMinDryAndMaxWet()
    {
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Nawa C", null);
        nawa.UpdateMoistureThresholds(30m, 70m);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var sensor1 = Sensor.Register("s-a");
        sensor1.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(sensor1, CancellationToken.None);
        var sensor2 = Sensor.Register("s-b");
        sensor2.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(sensor2, CancellationToken.None);

        readingRepo.Add(SensorReading.Create("s-a", DateTime.UtcNow.AddMinutes(-5), "t", "{}", 10m, 22m, 95, 100, sensor1.Id));
        readingRepo.Add(SensorReading.Create("s-b", DateTime.UtcNow.AddMinutes(-3), "t", "{}", 85m, 22m, 95, 100, sensor2.Id));

        var sut = new GetDashboardQueryService(nawaRepo, sensorRepo, readingRepo, NullLogger<GetDashboardQueryService>.Instance);
        var result = await sut.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(OperatorStatus.Conflict, result[0].Status);
        Assert.Equal(75m, result[0].MoistureSpread);
    }

    private sealed class InMemoryNawaRepo : INawaRepository
    {
        private readonly List<Nawa> _items = [];

        public Task AddAsync(Nawa nawa, CancellationToken ct) { _items.Add(nawa); return Task.CompletedTask; }
        public Task<Nawa?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(n => n.Id == id));
        public Task<IReadOnlyList<Nawa>> ListAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<Nawa>)_items);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class InMemorySensorRepo : ISensorRepository
    {
        private readonly List<Sensor> _items = [];

        public Task AddAsync(Sensor sensor, CancellationToken ct) { _items.Add(sensor); return Task.CompletedTask; }
        public Task<Sensor?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(s => s.Id == id));
        public Task<Sensor?> GetByExternalIdAsync(string externalId, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(s => s.ExternalId == externalId));
        public Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<Sensor>)_items);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class InMemoryReadingRepo : ISensorReadingRepository
    {
        private readonly List<SensorReading> _items = [];

        public void Add(SensorReading r) => _items.Add(r);

        public Task AddAsync(SensorReading reading, CancellationToken ct) { _items.Add(reading); return Task.CompletedTask; }
        public Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<SensorReading>)_items.OrderByDescending(r => r.ReceivedAtUtc).Take(count).ToList());

        public Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(IReadOnlyList<Guid> sensorIds, DateTime from, DateTime to, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<SensorReading>)_items
                .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value) && r.ReceivedAtUtc >= from && r.ReceivedAtUtc <= to)
                .OrderBy(r => r.ReceivedAtUtc).ToList());

        public Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(IReadOnlyList<Guid> sensorIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<SensorReading>)_items
                .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value))
                .GroupBy(r => r.SensorId).Select(g => g.OrderByDescending(r => r.ReceivedAtUtc).First()).ToList());
    }
}
