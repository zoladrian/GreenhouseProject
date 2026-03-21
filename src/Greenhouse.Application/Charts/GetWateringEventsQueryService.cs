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
            return [];

        var readings = await _readings.GetBySensorIdsAsync(sensorIds, from, to, cancellationToken);
        var grouped = readings.GroupBy(r => r.SensorId);

        var rawEvents = new List<(Guid SensorId, WateringEvent Event)>();

        foreach (var group in grouped)
        {
            if (!group.Key.HasValue)
                continue;

            var samples = group
                .Where(r => r.SoilMoisture.HasValue)
                .OrderBy(r => r.ReceivedAtUtc)
                .Select(r => new TimestampedMoisture(r.ReceivedAtUtc, r.SoilMoisture!.Value))
                .ToList();

            foreach (var e in WateringDetector.Detect(samples))
                rawEvents.Add((group.Key.Value, e));
        }

        var episodes = WateringEpisodeClusterer.Cluster(rawEvents);

        return episodes
            .Select(ep => new WateringEventDto(
                ep.DetectedAtUtc,
                ep.MoistureBefore,
                ep.MoistureAfter,
                ep.DeltaMoisture,
                ep.WindowDuration,
                ToApiKind(ep.InferredKind),
                ep.ContributingSensorCount))
            .ToList();
    }

    /// <summary>Ostatni epizod silnego skoku wilgotności w oknie (heurystyka podlania / deszczu).</summary>
    public async Task<WateringEventDto?> TryGetLastWateringEventAsync(
        Guid nawaId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var list = await ExecuteAsync(nawaId, fromUtc, toUtc, cancellationToken);
        if (list.Count == 0)
            return null;

        return list.OrderByDescending(e => e.DetectedAtUtc).First();
    }

    private static string ToApiKind(WateringEventInferredKind kind) =>
        kind switch
        {
            WateringEventInferredKind.LikelyRain => "likelyRain",
            WateringEventInferredKind.LikelyManual => "likelyManual",
            _ => "unknown"
        };
}
