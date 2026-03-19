using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Abstractions;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading, CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(
        IReadOnlyList<Guid> sensorIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(
        IReadOnlyList<Guid> sensorIds,
        CancellationToken cancellationToken);
}
