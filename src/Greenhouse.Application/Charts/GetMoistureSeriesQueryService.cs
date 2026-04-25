using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Charts;

public sealed class GetMoistureSeriesQueryService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetMoistureSeriesQueryService(ISensorRepository sensors, ISensorReadingRepository readings)
    {
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<IReadOnlyList<MoistureSeriesPointDto>> ExecuteAsync(
        Guid? nawaId,
        Guid? sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var sensorIds = await ResolveSensorIds(nawaId, sensorId, cancellationToken);
        if (sensorIds.Count == 0)
        {
            return [];
        }

        var readings = await _readings.GetBySensorIdsAsync(sensorIds, from, to, cancellationToken);

        return readings
            .OrderBy(r => r.ReceivedAtUtc)
            .Select(r => new MoistureSeriesPointDto(
                r.ReceivedAtUtc,
                r.SensorIdentifier,
                r.SensorId,
                r.SoilMoisture,
                r.Temperature,
                r.Battery,
                r.LinkQuality))
            .ToList();
    }

    private async Task<IReadOnlyList<Guid>> ResolveSensorIds(
        Guid? nawaId,
        Guid? sensorId,
        CancellationToken cancellationToken)
    {
        if (sensorId.HasValue)
        {
            return [sensorId.Value];
        }

        if (nawaId.HasValue)
        {
            var all = await _sensors.ListAsync(cancellationToken);
            return new NawaChartSensorScope(all).ResolveMoistureEnvironmentAndGlobalWeatherSensorIds(nawaId.Value);
        }

        return [];
    }
}
