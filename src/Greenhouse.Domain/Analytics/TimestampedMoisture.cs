namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Punkt pomiarowy wilgotności z timestampem — wejście do kalkulatorów.
/// </summary>
public readonly record struct TimestampedMoisture(DateTime UtcTime, decimal Moisture);
