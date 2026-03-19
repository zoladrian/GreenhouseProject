namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Analizuje efekt podlania: jak bardzo skok wilgotności różni się od historycznego.
/// </summary>
public static class WateringEffectAnalyzer
{
    public static WateringEffectSummary? Summarize(IReadOnlyList<WateringEvent> events)
    {
        if (events.Count == 0)
        {
            return null;
        }

        var deltas = events.Select(e => e.DeltaMoisture).ToList();

        return new WateringEffectSummary(
            AvgDelta: Math.Round(deltas.Average(), 2),
            MinDelta: deltas.Min(),
            MaxDelta: deltas.Max(),
            EventCount: events.Count);
    }
}
