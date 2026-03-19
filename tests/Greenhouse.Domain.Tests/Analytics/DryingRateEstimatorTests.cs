using Greenhouse.Domain.Analytics;

namespace Greenhouse.Domain.Tests.Analytics;

public sealed class DryingRateEstimatorTests
{
    private static readonly DateTime T0 = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Estimate_ShouldCalculatePercentPerHour()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 60m),
            new(T0.AddHours(1), 55m),
            new(T0.AddHours(2), 50m)
        };

        var result = DryingRateEstimator.Estimate(samples);

        Assert.NotNull(result);
        Assert.Equal(5m, result!.PercentPerHour);
    }

    [Fact]
    public void Estimate_ShouldReturnNull_ForSingleSample()
    {
        var result = DryingRateEstimator.Estimate([new(T0, 50m)]);
        Assert.Null(result);
    }

    [Fact]
    public void Estimate_ShouldReturnNull_ForTooShortWindow()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 60m),
            new(T0.AddMinutes(5), 59m)
        };

        var result = DryingRateEstimator.Estimate(samples);
        Assert.Null(result);
    }

    [Fact]
    public void Estimate_NegativeRate_MeansRisingMoisture()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 30m),
            new(T0.AddHours(1), 40m)
        };

        var result = DryingRateEstimator.Estimate(samples);

        Assert.NotNull(result);
        Assert.True(result!.PercentPerHour < 0);
    }
}
