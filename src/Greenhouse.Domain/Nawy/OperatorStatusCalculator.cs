namespace Greenhouse.Domain.Nawy;

public static class OperatorStatusCalculator
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(60);

    public static OperatorStatus Calculate(
        int sensorCount,
        decimal? avgMoisture,
        DateTime? oldestReadingUtc,
        MoistureThresholds moistureThresholds,
        DateTime utcNow)
    {
        if (sensorCount == 0 || !oldestReadingUtc.HasValue || !avgMoisture.HasValue)
        {
            return OperatorStatus.NoData;
        }

        if (utcNow - oldestReadingUtc.Value > StalenessThreshold)
        {
            return OperatorStatus.NoData;
        }

        if (moistureThresholds.IsBelowMin(avgMoisture.Value))
        {
            return OperatorStatus.Dry;
        }

        if (moistureThresholds.IsAboveMax(avgMoisture.Value))
        {
            return OperatorStatus.Warning;
        }

        return OperatorStatus.Ok;
    }
}
