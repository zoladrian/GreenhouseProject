using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Analytics;

namespace Greenhouse.Application.Charts;

public sealed class GetWateringEventsQueryService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetWateringEventsQueryService(ISensorRepository sensors, ISensorReadingRepository readings)
    {
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<IReadOnlyList<WateringEventDto>> ExecuteAsync(
        Guid nawaId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var allSensors = await _sensors.ListAsync(cancellationToken);
        var sensorIds = allSensors
            .Where(s => s.NawaId == nawaId)
            .Select(s => s.Id)
            .ToList();

        if (sensorIds.Count == 0)
        {
            return [];
        }

        var readings = await _readings.GetBySensorIdsAsync(sensorIds, from, to, cancellationToken);
        var grouped = readings.GroupBy(r => r.SensorId);

        var allEvents = new List<WateringEventDto>();

        foreach (var group in grouped)
        {
            var samples = group
                .Where(r => r.SoilMoisture.HasValue)
                .OrderBy(r => r.ReceivedAtUtc)
                .Select(r => new TimestampedMoisture(r.ReceivedAtUtc, r.SoilMoisture!.Value))
                .ToList();

            var detected = WateringDetector.Detect(samples);

            allEvents.AddRange(detected.Select(e => new WateringEventDto(
                e.DetectedAtUtc,
                e.MoistureBefore,
                e.MoistureAfter,
                e.DeltaMoisture,
                e.WindowDuration)));
        }

        return allEvents.OrderBy(e => e.DetectedAtUtc).ToList();
    }
}
