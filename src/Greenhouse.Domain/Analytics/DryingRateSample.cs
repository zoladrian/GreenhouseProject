namespace Greenhouse.Domain.Analytics;

public sealed record DryingRateSample(
    DateTime WindowStart,
    DateTime WindowEnd,
    decimal PercentPerHour);
