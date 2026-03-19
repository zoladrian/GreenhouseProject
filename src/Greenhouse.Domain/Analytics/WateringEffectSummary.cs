namespace Greenhouse.Domain.Analytics;

public sealed record WateringEffectSummary(
    decimal AvgDelta,
    decimal MinDelta,
    decimal MaxDelta,
    int EventCount);
