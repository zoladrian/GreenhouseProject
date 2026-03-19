using Greenhouse.Domain.Analytics;

namespace Greenhouse.Domain.Tests.Analytics;

public sealed class WateringDetectorTests
{
    private static readonly DateTime T0 = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Detect_ShouldFindSuddenSpike()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(10), 22m),
            new(T0.AddMinutes(15), 45m),
            new(T0.AddMinutes(20), 46m)
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m);

        Assert.Single(events);
        Assert.Equal(23m, events[0].DeltaMoisture);
        Assert.Equal(22m, events[0].MoistureBefore);
        Assert.Equal(45m, events[0].MoistureAfter);
    }

    [Fact]
    public void Detect_ShouldIgnoreSlowRise()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddHours(2), 35m)
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m, maxWindow: TimeSpan.FromMinutes(30));

        Assert.Empty(events);
    }

    [Fact]
    public void Detect_ShouldReturnEmpty_ForSingleSample()
    {
        var events = WateringDetector.Detect([new(T0, 30m)]);
        Assert.Empty(events);
    }

    [Fact]
    public void Detect_ShouldFindMultipleEvents()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(5), 35m),
            new(T0.AddMinutes(60), 25m),
            new(T0.AddMinutes(65), 42m)
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m);

        Assert.Equal(2, events.Count);
    }
}
