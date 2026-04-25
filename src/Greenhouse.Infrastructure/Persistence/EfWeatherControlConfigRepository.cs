using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Weather;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfWeatherControlConfigRepository : IWeatherControlConfigRepository
{
    private readonly GreenhouseDbContext _dbContext;

    public EfWeatherControlConfigRepository(GreenhouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WeatherControlConfig> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var row = await _dbContext.WeatherControlConfigs.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (row is not null)
            return row;

        row = WeatherControlConfig.CreateDefault();
        await _dbContext.WeatherControlConfigs.AddAsync(row, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return row;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
