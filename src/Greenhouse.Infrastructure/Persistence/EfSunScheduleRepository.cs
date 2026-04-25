using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Weather;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfSunScheduleRepository : ISunScheduleRepository
{
    private readonly GreenhouseDbContext _dbContext;

    public EfSunScheduleRepository(GreenhouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SunScheduleEntry?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken)
        => _dbContext.SunScheduleEntries.SingleOrDefaultAsync(x => x.Date == date, cancellationToken);

    public async Task<IReadOnlyList<SunScheduleEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        return await _dbContext.SunScheduleEntries
            .AsNoTracking()
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertManyAsync(IReadOnlyList<SunScheduleEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0) return;

        var min = entries.Min(x => x.Date);
        var max = entries.Max(x => x.Date);
        var existing = await _dbContext.SunScheduleEntries
            .Where(x => x.Date >= min && x.Date <= max)
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        foreach (var e in entries)
        {
            if (existing.TryGetValue(e.Date, out var row))
            {
                row.UpdateTimes(e.SunriseLocal, e.SunsetLocal);
            }
            else
            {
                await _dbContext.SunScheduleEntries.AddAsync(SunScheduleEntry.Create(e.Date, e.SunriseLocal, e.SunsetLocal), cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
