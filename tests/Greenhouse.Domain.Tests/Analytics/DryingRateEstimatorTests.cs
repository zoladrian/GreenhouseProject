using Greenhouse.Domain.Analytics;

namespace Greenhouse.Domain.Tests.Analytics;

public sealed class DryingRateEstimatorTests
{
    private static readonly DateTime T0 = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Estimate_ShouldCalculatePercentPerHour_ForLinearDrying()
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
    public void Estimate_ShouldReturnNull_WhenMidWindowRiseExceedsTolerance()
    {
        // 60 → 80 → 50 — w środku jest podlanie (+20). Stary "first-last" zwracał +5%/h
        // pozornego wysychania; nowy poprawnie zwraca null bo to nie jest okres bez podlania.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 60m),
            new(T0.AddMinutes(20), 80m),
            new(T0.AddMinutes(60), 50m)
        };

        var result = DryingRateEstimator.Estimate(samples);

        Assert.Null(result);
    }

    [Fact]
    public void Estimate_ShouldAccept_SmallNoiseRise_BelowTolerance()
    {
        // Szum czujnika: 60 → 60.5 → 55 (wzrost 0.5 < tolerance 1.5).
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 60m),
            new(T0.AddMinutes(20), 60.5m),
            new(T0.AddMinutes(60), 55m)
        };

        var result = DryingRateEstimator.Estimate(samples);

        Assert.NotNull(result);
        Assert.True(result!.PercentPerHour > 0, "wilgotność netto spadła, więc rate powinno być dodatnie");
    }

    [Fact]
    public void Estimate_LinearRegression_IsRobust_AgainstSingleNoiseSample()
    {
        // Niemal liniowe wysychanie 60 → 50 przez 1h, ale jeden lekko zaszumiony pomiar.
        // Regresja liniowa powinna dać wynik bliski 10%/h, niezniekształcony przez szum.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 60m),
            new(T0.AddMinutes(15), 57.4m),
            new(T0.AddMinutes(30), 55m),
            new(T0.AddMinutes(45), 52.6m),
            new(T0.AddMinutes(60), 50m),
        };

        var result = DryingRateEstimator.Estimate(samples);

        Assert.NotNull(result);
        Assert.InRange(result!.PercentPerHour, 9.5m, 10.5m);
    }
}
