namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Wykrywa epizody nagłego wzrostu wilgotności (prawdopodobne podlanie / deszcz).
///
/// Algorytm — sliding window:
/// 1. Dla bieżącej pozycji <c>i</c> szuka lokalnego minimum w oknie czasowym <paramref name="maxWindow"/>
///    (jeśli pojawia się niższy odczyt, kotwica się przesuwa).
/// 2. Pierwszy odczyt, dla którego skumulowany wzrost od minimum przekracza <paramref name="minDelta"/>,
///    triggeruje epizod.
/// 3. Epizod „rozszerza się” w prawo dopóki wartości rosną (lub plateau) i nadal mieszczą się w oknie.
///    Dzięki temu jedno podlanie rozłożone na 5 odczytów tworzy JEDEN epizod (a nie kilka nakładających).
/// 4. Po wyemitowaniu zdarzenia kontynuujemy od pozycji za szczytem epizodu.
///
/// W przeciwieństwie do prostego porównania par sąsiednich (poprzednia wersja) ten algorytm
/// poprawnie łapie powolne podlanie (Z2M co ~minutę → 5×+5%) i nie generuje overlapów.
/// </summary>
public static class WateringDetector
{
    private const decimal DefaultMinDelta = 5m;
    private static readonly TimeSpan DefaultMaxWindow = TimeSpan.FromMinutes(30);

    /// <param name="samples">Posortowane rosnąco po UtcTime.</param>
    /// <param name="minDelta">Minimalny skumulowany skok wilgotności w oknie.</param>
    /// <param name="maxWindow">Maksymalna szerokość okna pomiarowego (od lokalnego minimum do szczytu).</param>
    public static IReadOnlyList<WateringEvent> Detect(
        IReadOnlyList<TimestampedMoisture> samples,
        decimal minDelta = DefaultMinDelta,
        TimeSpan? maxWindow = null)
    {
        var window = maxWindow ?? DefaultMaxWindow;
        if (samples.Count < 2 || window <= TimeSpan.Zero || minDelta <= 0)
        {
            return [];
        }

        var events = new List<WateringEvent>();
        var i = 0;
        var n = samples.Count;

        while (i < n - 1)
        {
            var minIdx = i;
            var minVal = samples[i].Moisture;
            var triggerIdx = -1;

            for (var j = i + 1; j < n; j++)
            {
                var elapsed = samples[j].UtcTime - samples[minIdx].UtcTime;
                if (elapsed > window)
                {
                    break;
                }

                if (samples[j].Moisture < minVal)
                {
                    minIdx = j;
                    minVal = samples[j].Moisture;
                    continue;
                }

                if (samples[j].Moisture - minVal >= minDelta)
                {
                    triggerIdx = j;
                    break;
                }
            }

            if (triggerIdx < 0)
            {
                i++;
                continue;
            }

            var peakIdx = triggerIdx;
            for (var k = triggerIdx + 1; k < n; k++)
            {
                if (samples[k].UtcTime - samples[minIdx].UtcTime > window)
                    break;
                if (samples[k].Moisture >= samples[peakIdx].Moisture)
                    peakIdx = k;
                else
                    break;
            }

            events.Add(new WateringEvent(
                DetectedAtUtc: samples[peakIdx].UtcTime,
                MoistureBefore: minVal,
                MoistureAfter: samples[peakIdx].Moisture,
                DeltaMoisture: samples[peakIdx].Moisture - minVal,
                WindowDuration: samples[peakIdx].UtcTime - samples[minIdx].UtcTime));

            i = peakIdx + 1;
        }

        return events;
    }
}
