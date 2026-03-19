using Greenhouse.Domain.Analytics;

namespace Greenhouse.Domain.Tests.Analytics;

public sealed class WateringEffectAnalyzerTests
{
    [Fact]
    public void Summarize_ShouldCalculateStats()
    {
        var events = new List<WateringEvent>
        {
            new(DateTime.UtcNow, 20m, 40m, 20m, TimeSpan.FromMinutes(5)),
            new(DateTime.UtcNow, 25m, 55m, 30m, TimeSpan.FromMinutes(10)),
            new(DateTime.UtcNow, 30m, 45m, 15m, TimeSpan.FromMinutes(8))
        };

        var result = WateringEffectAnalyzer.Summarize(events);

        Assert.NotNull(result);
        Assert.Equal(3, result!.EventCount);
        Assert.Equal(15m, result.MinDelta);
        Assert.Equal(30m, result.MaxDelta);
        Assert.Equal(21.67m, result.AvgDelta);
    }

    [Fact]
    public void Summarize_ShouldReturnNull_ForEmptyList()
    {
        var result = WateringEffectAnalyzer.Summarize([]);
        Assert.Null(result);
    }
}
