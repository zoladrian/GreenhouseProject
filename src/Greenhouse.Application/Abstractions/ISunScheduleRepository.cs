using Greenhouse.Domain.Weather;

namespace Greenhouse.Application.Abstractions;

public interface ISunScheduleRepository
{
    Task<SunScheduleEntry?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<SunScheduleEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task UpsertManyAsync(IReadOnlyList<SunScheduleEntry> entries, CancellationToken cancellationToken);
}
