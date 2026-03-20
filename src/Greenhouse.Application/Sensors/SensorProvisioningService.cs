using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Sensors;

public sealed class SensorProvisioningService : ISensorProvisioningService
{
    private readonly ISensorRepository _sensors;

    public SensorProvisioningService(ISensorRepository sensors)
    {
        _sensors = sensors;
    }

    public async Task<SensorEnsureResult> EnsureSensorAsync(string mqttIdentifier, CancellationToken cancellationToken)
    {
        var existing = await _sensors.GetByExternalIdAsync(mqttIdentifier, cancellationToken);
        if (existing is not null)
        {
            return new SensorEnsureResult(existing.Id, CreatedNew: false);
        }

        var sensor = Sensor.Register(mqttIdentifier);
        await _sensors.AddAsync(sensor, cancellationToken);
        return new SensorEnsureResult(sensor.Id, CreatedNew: true);
    }
}
