namespace Greenhouse.Application.Charts;

/// <param name="InferredKind"><c>likelyManual</c> | <c>likelyRain</c> | <c>unknown</c> (heurystyka).</param>
public sealed record WateringEventDto(
    DateTime DetectedAtUtc,
    decimal MoistureBefore,
    decimal MoistureAfter,
    decimal DeltaMoisture,
    TimeSpan WindowDuration,
    string InferredKind,
    int ContributingSensorCount);
