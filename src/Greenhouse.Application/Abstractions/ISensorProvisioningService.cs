namespace Greenhouse.Application.Abstractions;

/// <summary>
/// Zapewnia istnienie rekordu czujnika dla identyfikatora z MQTT (upsert po ExternalId).
/// </summary>
public interface ISensorProvisioningService
{
    Task<SensorEnsureResult> EnsureSensorAsync(string mqttIdentifier, CancellationToken cancellationToken);
}
