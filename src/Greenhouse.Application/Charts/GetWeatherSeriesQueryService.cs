using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Weather;

namespace Greenhouse.Application.Charts;

public sealed class GetWeatherSeriesQueryService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;
    private readonly WeatherInterpretationService _interpretation;

    public GetWeatherSeriesQueryService(
        ISensorRepository sensors,
        ISensorReadingRepository readings,
        WeatherInterpretationService interpretation)
    {
        _sensors = sensors;
        _readings = readings;
        _interpretation = interpretation;
    }

    public async Task<IReadOnlyList<WeatherSeriesPointDto>> ExecuteAsync(
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
        var bySensor = readings
            .Where(r => r.SensorId.HasValue)
            .GroupBy(r => r.SensorId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.ReceivedAtUtc).ToList());

        var result = new List<WeatherSeriesPointDto>(readings.Count);
        foreach (var row in readings.OrderBy(r => r.ReceivedAtUtc))
        {
            RainLevel? rainLevel = null;
            LightLevel? lightLevel = null;

            if (row.SensorId.HasValue && bySensor.TryGetValue(row.SensorId.Value, out var history))
            {
                var uptoNow = history.Where(x => x.ReceivedAtUtc <= row.ReceivedAtUtc).ToList();
                var interpreted = _interpretation.Interpret(row, uptoNow, row.ReceivedAtUtc);
                rainLevel = interpreted.RainLevel;
                lightLevel = interpreted.LightLevel;
            }

            result.Add(new WeatherSeriesPointDto(
                row.ReceivedAtUtc,
                row.SensorIdentifier,
                row.SensorId,
                row.Rain,
                row.RainIntensityRaw,
                row.IlluminanceRaw,
                row.IlluminanceAverage20MinRaw,
                row.IlluminanceMaximumTodayRaw,
                row.Battery,
                row.LinkQuality,
                row.CleaningReminder,
                rainLevel,
                lightLevel));
        }

        return result;
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
            return all.Where(s => s.NawaId == nawaId.Value).Select(s => s.Id).ToList();
        }

        return [];
    }
}
