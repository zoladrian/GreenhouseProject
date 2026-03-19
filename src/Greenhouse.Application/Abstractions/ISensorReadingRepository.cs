using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Abstractions;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading, CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken);
}
