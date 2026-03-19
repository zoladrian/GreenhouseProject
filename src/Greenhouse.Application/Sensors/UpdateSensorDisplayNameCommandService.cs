using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Sensors;

public sealed class UpdateSensorDisplayNameCommandService
{
    private readonly ISensorRepository _sensors;

    public UpdateSensorDisplayNameCommandService(ISensorRepository sensors)
    {
        _sensors = sensors;
    }

    public async Task<SensorListItemDto?> ExecuteAsync(
        Guid sensorId,
        string? displayName,
        CancellationToken cancellationToken)
    {
        var sensor = await _sensors.GetByIdAsync(sensorId, cancellationToken);
        if (sensor is null)
        {
            return null;
        }

        sensor.SetDisplayName(displayName);
        await _sensors.SaveChangesAsync(cancellationToken);

        return new SensorListItemDto(
            sensor.Id, sensor.ExternalId, sensor.DisplayName,
            sensor.NawaId, sensor.CreatedAtUtc);
    }
}
