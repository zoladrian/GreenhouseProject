using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Sensors;

public sealed class DeleteSensorCommandService
{
    private readonly ISensorRepository _sensors;

    public DeleteSensorCommandService(ISensorRepository sensors)
    {
        _sensors = sensors;
    }

    public Task<bool> ExecuteAsync(Guid sensorId, CancellationToken cancellationToken) =>
        _sensors.DeleteAsync(sensorId, cancellationToken);
}
