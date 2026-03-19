namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Wykrywa nagłe skoki wilgotności (prawdopodobne podlanie).
/// </summary>
public static class WateringDetector
{
    private const decimal DefaultMinDelta = 5m;
    private static readonly TimeSpan DefaultMaxWindow = TimeSpan.FromMinutes(30);

    /// <param name="samples">Posortowane rosnąco po UtcTime.</param>
    public static IReadOnlyList<WateringEvent> Detect(
        IReadOnlyList<TimestampedMoisture> samples,
        decimal minDelta = DefaultMinDelta,
        TimeSpan? maxWindow = null)
    {
        var window = maxWindow ?? DefaultMaxWindow;
        if (samples.Count < 2)
        {
            return [];
        }

        var events = new List<WateringEvent>();

        for (var i = 1; i < samples.Count; i++)
        {
            var prev = samples[i - 1];
            var curr = samples[i];
            var delta = curr.Moisture - prev.Moisture;
            var elapsed = curr.UtcTime - prev.UtcTime;

            if (delta >= minDelta && elapsed <= window && elapsed > TimeSpan.Zero)
            {
                events.Add(new WateringEvent(
                    DetectedAtUtc: curr.UtcTime,
                    MoistureBefore: prev.Moisture,
                    MoistureAfter: curr.Moisture,
                    DeltaMoisture: delta,
                    WindowDuration: elapsed));
            }
        }

        return events;
    }
}
