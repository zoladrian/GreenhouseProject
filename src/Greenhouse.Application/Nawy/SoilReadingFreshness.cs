using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Nawy;

/// <summary>
/// Wspólny helper świeżości danych glebowych — używany w dashboardzie i briefie głosowym,
/// żeby OperatorStatusCalculator dostawał dokładnie ten sam <c>oldestReadingUtc</c> w obu miejscach.
/// Liczy się tylko najstarszy <see cref="SensorReading.ReceivedAtUtc"/> spośród wpisów,
/// które faktycznie niosą wartość <see cref="SensorReading.SoilMoisture"/>.
/// Bateria-only / weather-only nie są nigdy „świeżą wilgotnością”.
/// </summary>
public static class SoilReadingFreshness
{
    public static DateTime? ResolveOldestSoilReading(IReadOnlyList<SensorReading> latestPerSensor)
    {
        ArgumentNullException.ThrowIfNull(latestPerSensor);

        DateTime? oldest = null;
        for (var i = 0; i < latestPerSensor.Count; i++)
        {
            var r = latestPerSensor[i];
            if (!r.SoilMoisture.HasValue) continue;
            if (oldest is null || r.ReceivedAtUtc < oldest.Value)
                oldest = r.ReceivedAtUtc;
        }
        return oldest;
    }
}
