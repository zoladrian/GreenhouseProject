using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Sensors;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfSensorRepository : ISensorRepository
{
    private readonly GreenhouseDbContext _dbContext;

    public EfSensorRepository(GreenhouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Sensor?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Sensor sensor, CancellationToken cancellationToken)
    {
        await _dbContext.Sensors.AddAsync(sensor, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
