namespace Greenhouse.Domain.Analytics;

public sealed record WateringEvent(
    DateTime DetectedAtUtc,
    decimal MoistureBefore,
    decimal MoistureAfter,
    decimal DeltaMoisture,
    TimeSpan WindowDuration);
