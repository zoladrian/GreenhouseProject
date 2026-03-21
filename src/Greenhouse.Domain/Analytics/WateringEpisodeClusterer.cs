namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Łączy wykryte skoki z wielu czujników w „epizody” i przypisuje heurystyczny rodzaj (podlanie vs deszcz).
/// </summary>
public static class WateringEpisodeClusterer
{
    /// <summary>Maksymalny odstęp czasu między dwoma skokami, żeby uznać je za ten sam epizod (np. deszcz pada kilka minut).</summary>
    public static readonly TimeSpan DefaultTimeLink = TimeSpan.FromMinutes(45);

    /// <param name="perSensorEvents">Wykryte zdarzenia z identyfikatorem czujnika w jednej nawie.</param>
    public static IReadOnlyList<ClusteredWateringEpisode> Cluster(
        IReadOnlyList<(Guid SensorId, WateringEvent Event)> perSensorEvents,
        TimeSpan? timeLink = null)
    {
        var link = timeLink ?? DefaultTimeLink;
        if (perSensorEvents.Count == 0)
            return [];

        var ordered = perSensorEvents.OrderBy(x => x.Event.DetectedAtUtc).ToList();
        var n = ordered.Count;
        var parent = Enumerable.Range(0, n).ToArray();

        int Find(int i)
        {
            if (parent[i] != i)
                parent[i] = Find(parent[i]);
            return parent[i];
        }

        void Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb)
                parent[rb] = ra;
        }

        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                var dt = ordered[j].Event.DetectedAtUtc - ordered[i].Event.DetectedAtUtc;
                if (dt <= link)
                    Union(i, j);
            }
        }

        var episodes = new List<ClusteredWateringEpisode>();

        foreach (var g in Enumerable.Range(0, n).GroupBy(Find))
        {
            var list = g.Select(idx => ordered[idx]).ToList();
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

        return episodes.OrderBy(e => e.DetectedAtUtc).ToList();
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
