namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Estymuje tempo wysychania (%/h) z szeregu spadających odczytów.
/// </summary>
public static class DryingRateEstimator
{
    private static readonly TimeSpan MinWindow = TimeSpan.FromMinutes(15);

    /// <param name="samples">Posortowane rosnąco po UtcTime. Powinny być w okresie bez podlania.</param>
    public static DryingRateSample? Estimate(IReadOnlyList<TimestampedMoisture> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var first = samples[0];
        var last = samples[^1];
        var elapsed = last.UtcTime - first.UtcTime;

        if (elapsed < MinWindow)
        {
            return null;
        }

        var delta = first.Moisture - last.Moisture;
        var hours = (decimal)elapsed.TotalHours;
        var ratePerHour = delta / hours;

        return new DryingRateSample(first.UtcTime, last.UtcTime, Math.Round(ratePerHour, 4));
    }
}
