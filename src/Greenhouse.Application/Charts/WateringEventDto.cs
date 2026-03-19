namespace Greenhouse.Application.Charts;

public sealed record WateringEventDto(
    DateTime DetectedAtUtc,
    decimal MoistureBefore,
    decimal MoistureAfter,
    decimal DeltaMoisture,
    TimeSpan WindowDuration);
