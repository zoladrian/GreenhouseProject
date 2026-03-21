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

    /// <summary>Odczytuje znormalizowany IEEE z ostatniego zapisu dla danego czujnika (JSON Z2M).</summary>
    Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken cancellationToken);

    /// <summary>
    /// Ustawia pole identyfikatora w odczytach przypisanych do <paramref name="sensorId"/> na <paramref name="externalId"/> (np. po rekey na IEEE).
    /// </summary>
    Task<int> AlignSensorIdentifierForSensorAsync(Guid sensorId, string externalId, CancellationToken cancellationToken);

    /// <summary>
    /// Dla każdego odczytu z ustawionym <c>SensorId</c> ustawia identyfikator na <c>ExternalId</c> powiązanego rekordu czujnika (idempotentne).
    /// </summary>
    Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken cancellationToken);
}
