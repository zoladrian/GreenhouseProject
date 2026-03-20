namespace Greenhouse.Domain.Nawy;

/// <summary>
/// Status operacyjny nawy na podstawie agregacji wilgotności: <b>min</b> dla suchoty, <b>max</b> dla przemoczenia,
/// rozstrzał dla niespójności czujników.
/// </summary>
public static class OperatorStatusCalculator
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(60);

    /// <summary>Domyślny próg rozstrzału (% pkt.) do statusu <see cref="OperatorStatus.UnevenMoisture"/>.</summary>
    public const decimal DefaultSpreadAlertPercent = 15m;

    public static OperatorStatus Calculate(
        int assignedSensorCount,
        int moistureReadingCount,
        decimal? minMoisture,
        decimal? maxMoisture,
        DateTime? oldestReadingUtc,
        MoistureThresholds moistureThresholds,
        DateTime utcNow,
        decimal spreadAlertPercent = DefaultSpreadAlertPercent)
    {
        if (assignedSensorCount == 0)
            return OperatorStatus.NoData;

        if (moistureReadingCount == 0 || !minMoisture.HasValue || !maxMoisture.HasValue || !oldestReadingUtc.HasValue)
            return OperatorStatus.NoData;

        if (utcNow - oldestReadingUtc.Value > StalenessThreshold)
            return OperatorStatus.NoData;

        var minV = minMoisture.Value;
        var maxV = maxMoisture.Value;
        var spread = maxV - minV;

        var hasMinThreshold = moistureThresholds.Min.HasValue;
        var hasMaxThreshold = moistureThresholds.Max.HasValue;

        if (!hasMinThreshold && !hasMaxThreshold)
        {
            if (moistureReadingCount >= 2 && spread >= spreadAlertPercent)
                return OperatorStatus.UnevenMoisture;
            return OperatorStatus.Ok;
        }

        var tooDry = hasMinThreshold && minV < moistureThresholds.Min!.Value;
        var tooWet = hasMaxThreshold && maxV > moistureThresholds.Max!.Value;

        if (tooDry && tooWet)
            return OperatorStatus.Conflict;

        if (tooDry)
            return OperatorStatus.Dry;

        if (tooWet)
            return OperatorStatus.Warning;

        if (moistureReadingCount >= 2 && spread >= spreadAlertPercent)
            return OperatorStatus.UnevenMoisture;

        return OperatorStatus.Ok;
    }
}
