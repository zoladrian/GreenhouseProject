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
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
