namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Łączy wykryte skoki z wielu czujników w „epizody” i przypisuje heurystyczny rodzaj
/// (podlanie vs deszcz). Epizod = grupa zdarzeń, które wystąpiły blisko siebie w czasie
/// (kotwica = początek epizodu, nowe zdarzenie dołącza jeśli mieści się w <paramref name="DefaultTimeLink"/>
/// od początku epizodu — bez transitive chain linking, gdzie 0/40/80 min stałyby się
/// jednym epizodem trwającym 80 min mimo limitu 45 min).
/// </summary>
public static class WateringEpisodeClusterer
{
    /// <summary>Maksymalna długość epizodu (od pierwszego do ostatniego zdarzenia w klastrze).</summary>
    public static readonly TimeSpan DefaultTimeLink = TimeSpan.FromMinutes(45);

    /// <param name="perSensorEvents">Wykryte zdarzenia z identyfikatorem czujnika w jednej nawie.</param>
    /// <param name="timeLink">Maksymalny dystans od kotwicy klastra (pierwszego zdarzenia w epizodzie).</param>
    public static IReadOnlyList<ClusteredWateringEpisode> Cluster(
        IReadOnlyList<(Guid SensorId, WateringEvent Event)> perSensorEvents,
        TimeSpan? timeLink = null)
    {
        var link = timeLink ?? DefaultTimeLink;
        if (perSensorEvents.Count == 0)
            return [];

        var ordered = perSensorEvents.OrderBy(x => x.Event.DetectedAtUtc).ToList();

        var clusters = new List<List<(Guid SensorId, WateringEvent Event)>>();
        List<(Guid SensorId, WateringEvent Event)>? current = null;
        DateTime? anchor = null;

        foreach (var item in ordered)
        {
            if (current is null || anchor is null || item.Event.DetectedAtUtc - anchor.Value > link)
            {
                current = new List<(Guid, WateringEvent)>();
                clusters.Add(current);
                anchor = item.Event.DetectedAtUtc;
            }
            current.Add(item);
        }

        var episodes = new List<ClusteredWateringEpisode>(clusters.Count);
        foreach (var list in clusters)
        {
            var distinctSensors = list.Select(x => x.SensorId).Distinct().Count();
            var kind = distinctSensors >= 2
                ? WateringEventInferredKind.LikelyRain
                : WateringEventInferredKind.LikelyManual;

            var latest = list.MaxBy(x => x.Event.DetectedAtUtc)!.Event;
            var minBefore = list.Min(x => x.Event.MoistureBefore);
            var maxAfter = list.Max(x => x.Event.MoistureAfter);
            var maxDelta = list.Max(x => x.Event.DeltaMoisture);
            var maxWindow = list.Max(x => x.Event.WindowDuration);

            episodes.Add(new ClusteredWateringEpisode(
                DetectedAtUtc: latest.DetectedAtUtc,
                MoistureBefore: minBefore,
                MoistureAfter: maxAfter,
                DeltaMoisture: maxDelta,
                WindowDuration: maxWindow,
                InferredKind: kind,
                ContributingSensorCount: distinctSensors));
        }

        return episodes;
    }
}

public sealed record ClusteredWateringEpisode(
    DateTime DetectedAtUtc,
    decimal MoistureBefore,
    decimal MoistureAfter,
    decimal DeltaMoisture,
    TimeSpan WindowDuration,
    WateringEventInferredKind InferredKind,
    int ContributingSensorCount);
