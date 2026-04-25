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
        var result = new List<WeatherSeriesPointDto>(readings.Count);
        foreach (var perSensor in readings
                     .Where(r => r.SensorId.HasValue)
                     .GroupBy(r => r.SensorId!.Value))
        {
            DateTime? rainStreakStart = null;
            foreach (var row in perSensor.OrderBy(r => r.ReceivedAtUtc))
            {
                if (row.Rain == true)
                {
                    rainStreakStart ??= row.ReceivedAtUtc;
                }
                else
                {
                    rainStreakStart = null;
                }

                var rainSignalMinutes = rainStreakStart.HasValue
                    ? (int)Math.Round((row.ReceivedAtUtc - rainStreakStart.Value).TotalMinutes)
                    : 0;
                var interpreted = _interpretation.Interpret(row, rainSignalMinutes);
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
                    interpreted.RainLevel,
                    interpreted.LightLevel));
            }
        }

        return result.OrderBy(x => x.UtcTime).ToList();
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
            return new NawaChartSensorScope(all).ResolveGlobalWeatherSensorIds();
        }

        return [];
    }
}
