using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Sensors;

public sealed class GetSensorHealthQueryService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetSensorHealthQueryService(ISensorRepository sensors, ISensorReadingRepository readings)
    {
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<IReadOnlyList<SensorHealthDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var sensors = await _sensors.ListAsync(cancellationToken);
        var sensorIds = sensors.Select(s => s.Id).ToList();

        var latestReadings = await _readings.GetLatestPerSensorAsync(sensorIds, cancellationToken);
        var last24h = await _readings.GetBySensorIdsAsync(
            sensorIds,
            DateTime.UtcNow.AddHours(-24),
            DateTime.UtcNow,
            cancellationToken);

        var countPerSensor = last24h
            .Where(r => r.SensorId.HasValue)
            .GroupBy(r => r.SensorId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return sensors.Select(sensor =>
        {
            var latest = latestReadings.FirstOrDefault(r => r.SensorId == sensor.Id);
            countPerSensor.TryGetValue(sensor.Id, out var count24h);

            return new SensorHealthDto(
                sensor.Id,
                sensor.ExternalId,
                sensor.DisplayName,
                sensor.NawaId,
                latest?.Battery,
                latest?.LinkQuality,
                latest?.ReceivedAtUtc,
                count24h);
        }).ToList();
    }
}
