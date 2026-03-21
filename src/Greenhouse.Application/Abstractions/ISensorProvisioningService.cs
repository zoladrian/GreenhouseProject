namespace Greenhouse.Application.Abstractions;

/// <summary>
/// Zapewnia rekord czujnika: klucz IEEE (z JSON) lub topic; scala ze starym wpisem po friendly name.
/// </summary>
public interface ISensorProvisioningService
{
    Task<SensorEnsureResult> EnsureSensorAsync(EnsureSensorInput input, CancellationToken cancellationToken);
}
