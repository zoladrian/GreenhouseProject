using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Sensors;

public sealed record SensorDetailDto(
    Guid Id,
    string ExternalId,
    string? DisplayName,
    Guid? NawaId,
    int? Battery,
    int? LinkQuality,
    decimal? LastMoisture,
    decimal? LastTemperature,
    DateTime? LastReadingUtc,
    DateTime CreatedAtUtc);

public sealed class GetSensorDetailQueryService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetSensorDetailQueryService(ISensorRepository sensors, ISensorReadingRepository readings)
    {
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<SensorDetailDto?> ExecuteAsync(Guid sensorId, CancellationToken cancellationToken)
    {
        var sensor = await _sensors.GetByIdAsync(sensorId, cancellationToken);
        if (sensor is null)
        {
            return null;
        }

        var latest = await _readings.GetLatestPerSensorAsync([sensorId], cancellationToken);
        var lastReading = latest.FirstOrDefault();

        return new SensorDetailDto(
            sensor.Id,
            sensor.ExternalId,
            sensor.DisplayName,
            sensor.NawaId,
            lastReading?.Battery,
            lastReading?.LinkQuality,
            lastReading?.SoilMoisture,
            lastReading?.Temperature,
            lastReading?.ReceivedAtUtc,
            sensor.CreatedAtUtc);
    }
}
