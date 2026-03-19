using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Sensors;

public sealed class AssignSensorToNawaCommandService
{
    private readonly ISensorRepository _sensors;
    private readonly INawaRepository _nawy;

    public AssignSensorToNawaCommandService(ISensorRepository sensors, INawaRepository nawy)
    {
        _sensors = sensors;
        _nawy = nawy;
    }

    public async Task<AssignSensorResult> ExecuteAsync(
        Guid sensorId,
        Guid? nawaId,
        CancellationToken cancellationToken)
    {
        var sensor = await _sensors.GetByIdAsync(sensorId, cancellationToken);
        if (sensor is null)
        {
            return AssignSensorResult.SensorNotFound;
        }

        if (nawaId.HasValue)
        {
            var nawa = await _nawy.GetByIdAsync(nawaId.Value, cancellationToken);
            if (nawa is null)
            {
                return AssignSensorResult.NawaNotFound;
            }

            sensor.AssignToNawa(nawaId.Value);
        }
        else
        {
            sensor.UnassignFromNawa();
        }

        await _sensors.SaveChangesAsync(cancellationToken);
        return AssignSensorResult.Ok;
    }
}

public enum AssignSensorResult
{
    Ok,
    SensorNotFound,
    NawaNotFound
}
