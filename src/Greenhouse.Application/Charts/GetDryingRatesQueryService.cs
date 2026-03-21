using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Analytics;

namespace Greenhouse.Application.Charts;

public sealed class GetDryingRatesQueryService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetDryingRatesQueryService(ISensorRepository sensors, ISensorReadingRepository readings)
    {
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<IReadOnlyList<DryingRateDto>> ExecuteAsync(
        Guid nawaId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var allSensors = await _sensors.ListAsync(cancellationToken);
        var nawaSensors = allSensors.Where(s => s.NawaId == nawaId).ToList();

        if (nawaSensors.Count == 0)
        {
            return [];
        }

        var sensorIds = nawaSensors.Select(s => s.Id).ToList();
        var readings = await _readings.GetBySensorIdsAsync(sensorIds, from, to, cancellationToken);

        var results = new List<DryingRateDto>();

        foreach (var sensor in nawaSensors)
        {
            var sensorReadings = readings
                .Where(r => r.SensorId == sensor.Id && r.SoilMoisture.HasValue)
                .OrderBy(r => r.ReceivedAtUtc)
                .Select(r => new TimestampedMoisture(r.ReceivedAtUtc, r.SoilMoisture!.Value))
                .ToList();

            var rate = DryingRateEstimator.Estimate(sensorReadings);
            if (rate is not null)
            {
                results.Add(new DryingRateDto(
                    sensor.DisplayName ?? sensor.ExternalId,
                    sensor.Id,
                    rate.WindowStart,
                    rate.WindowEnd,
                    rate.PercentPerHour));
            }
        }

        return results;
    }
}
