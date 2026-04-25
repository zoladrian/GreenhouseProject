namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Estymuje tempo wysychania (%/h) z szeregu odczytów wilgotności w ciągu czasu bez podlania.
///
/// Zmiany względem wersji „first/last”:
/// 1. Walidacja monotoniczności — jeżeli w środku okna jest istotny WZROST wilgotności
///    (większy niż <see cref="DefaultRiseTolerance"/>), to znaczy że ktoś podlał — zwracamy null.
///    Wcześniejsza wersja brała tylko punkty skrajne i mogła zwrócić bzdurnie niski drying rate.
/// 2. Regresja liniowa metodą najmniejszych kwadratów zamiast czystego (first - last) / time —
///    odporna na pojedyncze szumy i brakujące pomiary.
/// </summary>
public static class DryingRateEstimator
{
    private static readonly TimeSpan MinWindow = TimeSpan.FromMinutes(15);

    /// <summary>Tolerancja chwilowego wzrostu wilgotności między sąsiadami (np. szum czujnika).</summary>
    public const decimal DefaultRiseTolerance = 1.5m;

    /// <param name="samples">Posortowane rosnąco po UtcTime. Powinny być w okresie bez podlania.</param>
    /// <param name="riseTolerance">
    /// Maksymalny dopuszczalny wzrost wilgotności między sąsiednimi pomiarami.
    /// Jeżeli choć jeden krok przekracza próg, zakładamy podlanie i zwracamy null.
    /// </param>
    public static DryingRateSample? Estimate(
        IReadOnlyList<TimestampedMoisture> samples,
        decimal? riseTolerance = null)
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

        var tolerance = riseTolerance ?? DefaultRiseTolerance;
        for (var i = 1; i < samples.Count; i++)
        {
            var rise = samples[i].Moisture - samples[i - 1].Moisture;
            if (rise > tolerance)
            {
                // W środku okna jest istotny wzrost — to nie jest okres czystego wysychania.
                return null;
            }
        }

        // Regresja liniowa metodą najmniejszych kwadratów: y = a*x + b, gdzie x to sekundy od first.UtcTime.
        // slope (a) jest w jednostce % / s; pomnożone przez 3600 daje % / h. Drying rate jest dodatnie
        // gdy wilgotność spada, więc bierzemy -slope.
        double sumX = 0;
        double sumY = 0;
        double sumXX = 0;
        double sumXY = 0;
        var n = samples.Count;

        for (var i = 0; i < n; i++)
        {
            var x = (samples[i].UtcTime - first.UtcTime).TotalSeconds;
            var y = (double)samples[i].Moisture;
            sumX += x;
            sumY += y;
            sumXX += x * x;
            sumXY += x * y;
        }

        var denominator = n * sumXX - sumX * sumX;
        if (denominator == 0)
        {
            return null;
        }

        var slopePerSecond = (n * sumXY - sumX * sumY) / denominator;
        var ratePerHour = (decimal)(-slopePerSecond * 3600.0);

        return new DryingRateSample(first.UtcTime, last.UtcTime, Math.Round(ratePerHour, 4));
    }
}
