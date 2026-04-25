using Greenhouse.Application.Charts;

namespace Greenhouse.Application.Tests.Charts;

public sealed class SeriesDecimatorTests
{
    private sealed record Point(string SeriesKey, DateTime Time, int Value);

    [Fact]
    public void SampleMaxPerSeries_ReturnsAll_WhenUnderLimit()
    {
        var input = new List<Point>
        {
            new("a", new(2026,4,25,10,0,0,DateTimeKind.Utc), 1),
            new("b", new(2026,4,25,10,5,0,DateTimeKind.Utc), 2),
        };

        var output = SeriesDecimator.SampleMaxPerSeries(input, p => p.SeriesKey, p => p.Time, 100);

        Assert.Equal(2, output.Count);
    }

    [Fact]
    public void SampleMaxPerSeries_LimitsEachSeriesIndependently()
    {
        var input = new List<Point>();
        for (var i = 0; i < 1000; i++)
            input.Add(new("a", new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i), i));
        for (var i = 0; i < 50; i++)
            input.Add(new("b", new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i), i));

        var output = SeriesDecimator.SampleMaxPerSeries(input, p => p.SeriesKey, p => p.Time, 100);

        var byKey = output.GroupBy(p => p.SeriesKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(100, byKey["a"]);
        Assert.Equal(50, byKey["b"]);
    }

    [Fact]
    public void SampleMaxPerSeries_PreservesGlobalChronologicalOrder()
    {
        var input = new List<Point>
        {
            new("a", new(2026,4,25,12,0,0,DateTimeKind.Utc), 1),
            new("b", new(2026,4,25,11,0,0,DateTimeKind.Utc), 2),
            new("a", new(2026,4,25,13,0,0,DateTimeKind.Utc), 3),
            new("b", new(2026,4,25,14,0,0,DateTimeKind.Utc), 4),
        };

        var output = SeriesDecimator.SampleMaxPerSeries(input, p => p.SeriesKey, p => p.Time, 100);

        Assert.True(output.Zip(output.Skip(1), (a, b) => a.Time <= b.Time).All(x => x));
    }

    [Fact]
    public void SampleMaxPerSeries_DoesNotMixSeries_RegressionAgainstFlatSampleMax()
    {
        // Stary algorytm tnący spłaszczoną listę gubił całe serie albo „mieszał”
        // punkty A i B w jednym kroku samplowania. Każda seria musi zachować swoje min/max.
        var input = new List<Point>();
        for (var i = 0; i < 200; i++)
            input.Add(new("a", new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i), i));
        for (var i = 0; i < 200; i++)
            input.Add(new("b", new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i), 1000 + i));

        var output = SeriesDecimator.SampleMaxPerSeries(input, p => p.SeriesKey, p => p.Time, 25);

        var minA = output.Where(p => p.SeriesKey == "a").Min(p => p.Value);
        var maxA = output.Where(p => p.SeriesKey == "a").Max(p => p.Value);
        var minB = output.Where(p => p.SeriesKey == "b").Min(p => p.Value);
        var maxB = output.Where(p => p.SeriesKey == "b").Max(p => p.Value);

        Assert.Equal(0, minA);
        Assert.True(maxA >= 190);
        Assert.True(minB >= 1000);
        Assert.True(maxB >= 1190);
    }
}
