using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Abstractions;

public interface ISensorRepository
{
    Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Sensor?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>Śledzona encja EF — do aktualizacji ExternalId / nazwy wyświetlanej.</summary>
    Task<Sensor?> GetByExternalIdForUpdateAsync(string externalId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(Sensor sensor, CancellationToken cancellationToken);

    /// <summary>Usuwa czujnik. Odczyty zostają w bazie z <c>SensorId = null</c> (historia topiców).</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
