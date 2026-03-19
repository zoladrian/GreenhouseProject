using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Sensors;
using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Tests.Sensors;

public sealed class AssignSensorToNawaCommandServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldAssign_WhenNawaExists()
    {
        var nawa = Nawa.Create("N1", null);
        var sensor = Sensor.Register("ext-1");
        var nawaRepo = new FakeNawaRepo(nawa);
        var sensorRepo = new FakeSensorRepo(sensor);
        var sut = new AssignSensorToNawaCommandService(sensorRepo, nawaRepo);

        var result = await sut.ExecuteAsync(sensor.Id, nawa.Id, CancellationToken.None);

        Assert.Equal(AssignSensorResult.Ok, result);
        Assert.Equal(nawa.Id, sensor.NawaId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenSensorMissing()
    {
        var nawa = Nawa.Create("N1", null);
        var nawaRepo = new FakeNawaRepo(nawa);
        var sensorRepo = new FakeSensorRepo();
        var sut = new AssignSensorToNawaCommandService(sensorRepo, nawaRepo);

        var result = await sut.ExecuteAsync(Guid.NewGuid(), nawa.Id, CancellationToken.None);

        Assert.Equal(AssignSensorResult.SensorNotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUnassign_WhenNawaIdNull()
    {
        var nawa = Nawa.Create("N1", null);
        var sensor = Sensor.Register("ext-1");
        sensor.AssignToNawa(nawa.Id);
        var nawaRepo = new FakeNawaRepo(nawa);
        var sensorRepo = new FakeSensorRepo(sensor);
        var sut = new AssignSensorToNawaCommandService(sensorRepo, nawaRepo);

        var result = await sut.ExecuteAsync(sensor.Id, null, CancellationToken.None);

        Assert.Equal(AssignSensorResult.Ok, result);
        Assert.Null(sensor.NawaId);
    }

    private sealed class FakeNawaRepo : INawaRepository
    {
        private readonly Dictionary<Guid, Nawa> _items;

        public FakeNawaRepo(params Nawa[] nawy)
        {
            _items = nawy.ToDictionary(x => x.Id);
        }

        public Task<Nawa?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Nawa>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Nawa>)_items.Values.ToList());

        public Task AddAsync(Nawa nawa, CancellationToken cancellationToken)
        {
            _items[nawa.Id] = nawa;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSensorRepo : ISensorRepository
    {
        private readonly Dictionary<Guid, Sensor> _byId = [];

        public FakeSensorRepo(params Sensor[] sensors)
        {
            foreach (var s in sensors)
            {
                _byId[s.Id] = s;
            }
        }

        public Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_byId.GetValueOrDefault(id));

        public Task<Sensor?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(_byId.Values.FirstOrDefault(s => s.ExternalId == externalId));

        public Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Sensor>)_byId.Values.ToList());

        public Task AddAsync(Sensor sensor, CancellationToken cancellationToken)
        {
            _byId[sensor.Id] = sensor;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
