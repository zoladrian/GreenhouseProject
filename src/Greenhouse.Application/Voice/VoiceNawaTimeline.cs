using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Voice;

/// <summary>
/// Szacuje moment rozpoczęcia stanu alarmowego na podstawie historii odczytów (okno czasowe, krok godzinowy).
/// </summary>
internal static class VoiceNawaTimeline
{
    public static DateTime FloorToHourUtc(DateTime utc) =>
        new(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc);

    public static Dictionary<Guid, List<SensorReading>> BuildPerSensorLists(
        IReadOnlyList<SensorReading> readings,
        IReadOnlyList<Guid> sensorIds)
    {
        var dict = sensorIds.ToDictionary(id => id, _ => new List<SensorReading>());
        foreach (var r in readings.OrderBy(x => x.ReceivedAtUtc))
        {
            if (r.SensorId is { } sid && dict.ContainsKey(sid))
                dict[sid].Add(r);
        }

        return dict;
    }

    public static decimal? MinMoistureAt(Dictionary<Guid, List<SensorReading>> perSensor, IReadOnlyList<Guid> sensorIds, DateTime instantUtc)
    {
        decimal? min = null;
        foreach (var id in sensorIds)
        {
            var last = LastReadingWithMoisture(perSensor[id], instantUtc);
            if (last?.SoilMoisture is null)
                return null;
            var v = last.SoilMoisture.Value;
            min = min is null ? v : Math.Min(min.Value, v);
        }

        return min;
    }

    public static decimal? MaxMoistureAt(Dictionary<Guid, List<SensorReading>> perSensor, IReadOnlyList<Guid> sensorIds, DateTime instantUtc)
    {
        decimal? max = null;
        foreach (var id in sensorIds)
        {
            var last = LastReadingWithMoisture(perSensor[id], instantUtc);
            if (last?.SoilMoisture is null)
                return null;
            var v = last.SoilMoisture.Value;
            max = max is null ? v : Math.Max(max.Value, v);
        }

        return max;
    }

    public static decimal? AvgTemperatureAt(Dictionary<Guid, List<SensorReading>> perSensor, IReadOnlyList<Guid> sensorIds, DateTime instantUtc)
    {
        var vals = new List<decimal>();
        foreach (var id in sensorIds)
        {
            var last = LastReadingWithTemperature(perSensor[id], instantUtc);
            if (last?.Temperature is null)
                return null;
            vals.Add(last.Temperature.Value);
        }

        return vals.Count == 0 ? null : Math.Round(vals.Average(), 2);
    }

    public static DateTime? EstimateMoistureDrySinceUtc(
        Dictionary<Guid, List<SensorReading>> perSensor,
        IReadOnlyList<Guid> sensorIds,
        decimal minThreshold,
        DateTime utcNow,
        TimeSpan lookback)
    {
        var atNow = MinMoistureAt(perSensor, sensorIds, utcNow);
        if (atNow is null || atNow >= minThreshold)
            return null;

        var from = utcNow - lookback;
        var startHour = FloorToHourUtc(from);
        DateTime? lastOkHour = null;

        for (var h = FloorToHourUtc(utcNow); h >= startHour; h = h.AddHours(-1))
        {
            var endOfHour = h.AddHours(1).AddTicks(-1);
            var min = MinMoistureAt(perSensor, sensorIds, endOfHour);
            if (min is null)
                continue;
            if (min >= minThreshold)
            {
                lastOkHour = h;
                break;
            }
        }

        return lastOkHour is null ? startHour : lastOkHour.Value.AddHours(1);
    }

    public static DateTime? EstimateMoistureWetSinceUtc(
        Dictionary<Guid, List<SensorReading>> perSensor,
        IReadOnlyList<Guid> sensorIds,
        decimal maxThreshold,
        DateTime utcNow,
        TimeSpan lookback)
    {
        var atNow = MaxMoistureAt(perSensor, sensorIds, utcNow);
        if (atNow is null || atNow <= maxThreshold)
            return null;

        var from = utcNow - lookback;
        var startHour = FloorToHourUtc(from);
        DateTime? lastOkHour = null;

        for (var h = FloorToHourUtc(utcNow); h >= startHour; h = h.AddHours(-1))
        {
            var endOfHour = h.AddHours(1).AddTicks(-1);
            var max = MaxMoistureAt(perSensor, sensorIds, endOfHour);
            if (max is null)
                continue;
            if (max <= maxThreshold)
            {
                lastOkHour = h;
                break;
            }
        }

        return lastOkHour is null ? startHour : lastOkHour.Value.AddHours(1);
    }

    /// <summary>
    /// Liczy, ile pełnych minut z rzędu wstecz od <paramref name="utcNow"/> stan „najwyższa wilgotność &gt; próg” jest spełniony.
    /// Gdy w danym momencie brakuje odczytu (null), nie uznajemy suchego — ciąg uznajemy za trwający, aż do limitu lookback.
    /// Zwraca <c>maxLookbackMinutes + 1</c>, jeśli w całym oknie nie znaleziono suchego.
    /// </summary>
    public static int EstimateContinuousTooWetMinutesFromNow(
        Dictionary<Guid, List<SensorReading>> perSensor,
        IReadOnlyList<Guid> sensorIdsWithMoisture,
        decimal maxThreshold,
        DateTime utcNow,
        int maxLookbackMinutes = 240)
    {
        var atNow = MaxMoistureAt(perSensor, sensorIdsWithMoisture, utcNow);
        if (atNow is null || atNow <= maxThreshold)
            return 0;

        for (var m = 0; m <= maxLookbackMinutes; m++)
        {
            var t = utcNow.AddMinutes(-m);
            var maxM = MaxMoistureAt(perSensor, sensorIdsWithMoisture, t);
            if (maxM.HasValue && maxM.Value <= maxThreshold)
                return m;
        }

        return maxLookbackMinutes + 1;
    }

    public static DateTime? EstimateTemperatureOutOfRangeSinceUtc(
        Dictionary<Guid, List<SensorReading>> perSensor,
        IReadOnlyList<Guid> sensorIds,
        decimal? tempMin,
        decimal? tempMax,
        DateTime utcNow,
        TimeSpan lookback)
    {
        if (!tempMin.HasValue && !tempMax.HasValue)
            return null;

        bool Out(decimal avg) =>
            (tempMin.HasValue && avg < tempMin.Value) || (tempMax.HasValue && avg > tempMax.Value);

        var atNow = AvgTemperatureAt(perSensor, sensorIds, utcNow);
        if (atNow is null || !Out(atNow.Value))
            return null;

        var from = utcNow - lookback;
        var startHour = FloorToHourUtc(from);
        DateTime? lastOkHour = null;

        for (var h = FloorToHourUtc(utcNow); h >= startHour; h = h.AddHours(-1))
        {
            var endOfHour = h.AddHours(1).AddTicks(-1);
            var avg = AvgTemperatureAt(perSensor, sensorIds, endOfHour);
            if (avg is null)
                continue;
            if (!Out(avg.Value))
            {
                lastOkHour = h;
                break;
            }
        }

        return lastOkHour is null ? startHour : lastOkHour.Value.AddHours(1);
    }

    private static SensorReading? LastReadingWithMoisture(IReadOnlyList<SensorReading> sortedAsc, DateTime instantUtc)
    {
        for (var i = sortedAsc.Count - 1; i >= 0; i--)
        {
            var r = sortedAsc[i];
            if (r.ReceivedAtUtc <= instantUtc && r.SoilMoisture.HasValue)
                return r;
        }

        return null;
    }

    private static SensorReading? LastReadingWithTemperature(IReadOnlyList<SensorReading> sortedAsc, DateTime instantUtc)
    {
        for (var i = sortedAsc.Count - 1; i >= 0; i--)
        {
            var r = sortedAsc[i];
            if (r.ReceivedAtUtc <= instantUtc && r.Temperature.HasValue)
                return r;
        }

        return null;
    }
}
