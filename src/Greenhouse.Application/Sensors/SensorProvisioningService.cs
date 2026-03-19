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

    public async Task<Guid> EnsureSensorAsync(string mqttIdentifier, CancellationToken cancellationToken)
    {
        var existing = await _sensors.GetByExternalIdAsync(mqttIdentifier, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var sensor = Sensor.Register(mqttIdentifier);
        await _sensors.AddAsync(sensor, cancellationToken);
        return sensor.Id;
    }
}
