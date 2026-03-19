using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.SensorReadings;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfSensorReadingRepository : ISensorReadingRepository
{
    private readonly GreenhouseDbContext _dbContext;

    public EfSensorReadingRepository(GreenhouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SensorReading reading, CancellationToken cancellationToken)
    {
        await _dbContext.SensorReadings.AddAsync(reading, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken)
    {
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(
        IReadOnlyList<Guid> sensorIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorId != null
                        && sensorIds.Contains(r.SensorId.Value)
                        && r.ReceivedAtUtc >= from
                        && r.ReceivedAtUtc <= to)
            .OrderBy(r => r.ReceivedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(
        IReadOnlyList<Guid> sensorIds,
        CancellationToken cancellationToken)
    {
        if (sensorIds.Count == 0)
        {
            return [];
        }

        var results = new List<SensorReading>();

        foreach (var sensorId in sensorIds)
        {
            var latest = await _dbContext.SensorReadings
                .AsNoTracking()
                .Where(r => r.SensorId == sensorId)
                .OrderByDescending(r => r.ReceivedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is not null)
            {
                results.Add(latest);
            }
        }

        return results;
    }
}
