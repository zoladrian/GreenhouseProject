namespace Greenhouse.Application.Charts;

/// <summary>
/// Decymacja punktów wykresu zachowująca kolejność czasową w obrębie serii (czujnika).
/// Krytyczne dla wykresów z wieloma czujnikami: stary <c>SampleMax</c> ucinał
/// całą zlepioną listę, przez co jedna seria mogła zostać kompletnie wycięta
/// albo punkty z różnych czujników mieszały się w jeden „pseudo-trend”.
/// </summary>
public static class SeriesDecimator
{
    /// <summary>
    /// Zwraca punkty pociągnięte do nie więcej niż <paramref name="maxPointsPerSeries"/>
    /// sztuk PER klucz serii. Klucz serii pochodzi z <paramref name="seriesKey"/>; wynik
    /// jest posortowany po <paramref name="timeKey"/>.
    /// </summary>
    public static IReadOnlyList<T> SampleMaxPerSeries<T, TKey>(
        IReadOnlyList<T> source,
        Func<T, TKey> seriesKey,
        Func<T, DateTime> timeKey,
        int maxPointsPerSeries)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(seriesKey);
        ArgumentNullException.ThrowIfNull(timeKey);

        if (source.Count == 0 || maxPointsPerSeries <= 0)
            return source;

        var groups = new Dictionary<TKey, List<T>>();
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            var key = seriesKey(item);
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<T>();
                groups[key] = list;
            }
            list.Add(item);
        }

        var output = new List<T>(Math.Min(source.Count, groups.Count * maxPointsPerSeries));
        foreach (var (_, list) in groups)
        {
            list.Sort((a, b) => timeKey(a).CompareTo(timeKey(b)));
            if (list.Count <= maxPointsPerSeries)
            {
                output.AddRange(list);
                continue;
            }

            var step = (double)list.Count / maxPointsPerSeries;
            for (var i = 0; i < maxPointsPerSeries; i++)
            {
                var idx = (int)Math.Floor(i * step);
                if (idx >= list.Count) idx = list.Count - 1;
                output.Add(list[idx]);
            }
        }

        output.Sort((a, b) => timeKey(a).CompareTo(timeKey(b)));
        return output;
    }
}
