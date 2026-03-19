using Greenhouse.Application.Sensors;
using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.Sensors;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.IntegrationTests.Nawy;

public sealed class NawaSensorAssignmentIntegrationTests
{
    [Fact]
    public async Task AssignSensor_ShouldPersistNawaId()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<GreenhouseDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new GreenhouseDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var nawaRepo = new EfNawaRepository(db);
        var sensorRepo = new EfSensorRepository(db);

        var nawa = Nawa.Create("Polowa 1", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var sensor = Sensor.Register("Czujnik wilgotnosci 1");
        await sensorRepo.AddAsync(sensor, CancellationToken.None);

        var assign = new AssignSensorToNawaCommandService(sensorRepo, nawaRepo);
        var result = await assign.ExecuteAsync(sensor.Id, nawa.Id, CancellationToken.None);

        Assert.Equal(AssignSensorResult.Ok, result);

        db.ChangeTracker.Clear();
        var reloaded = await db.Sensors.AsNoTracking().SingleAsync(x => x.Id == sensor.Id);
        Assert.Equal(nawa.Id, reloaded.NawaId);
    }
}
