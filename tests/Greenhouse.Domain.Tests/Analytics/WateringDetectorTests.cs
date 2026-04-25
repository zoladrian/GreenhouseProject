using Greenhouse.Domain.Analytics;

namespace Greenhouse.Domain.Tests.Analytics;

public sealed class WateringDetectorTests
{
    private static readonly DateTime T0 = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Detect_ShouldFindSuddenSpike_FromLocalMinimum_AndExtendThroughPeak()
    {
        // 20 -> 22 -> 45 -> 46. Sliding window łapie skok od lokalnego minimum (20),
        // a następnie rozszerza epizod przez peak (do 46) — to wciąż jedno podlanie.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(10), 22m),
            new(T0.AddMinutes(15), 45m),
            new(T0.AddMinutes(20), 46m)
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m);

        Assert.Single(events);
        Assert.Equal(26m, events[0].DeltaMoisture);
        Assert.Equal(20m, events[0].MoistureBefore);
        Assert.Equal(46m, events[0].MoistureAfter);
    }

    [Fact]
    public void Detect_ShouldFindCumulativeRise_AcrossManySmallSteps()
    {
        // Realny scenariusz: Z2M co minutę, podlewanie rozłożone na 5 odczytów po +5%.
        // Pairwise (stary): 5 < 10 → nic. Sliding window (nowy): 25 ≥ 10 → 1 epizod.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(1), 25m),
            new(T0.AddMinutes(2), 30m),
            new(T0.AddMinutes(3), 35m),
            new(T0.AddMinutes(4), 40m),
            new(T0.AddMinutes(5), 45m)
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m, maxWindow: TimeSpan.FromMinutes(30));

        Assert.Single(events);
        Assert.Equal(25m, events[0].DeltaMoisture);
    }

    [Fact]
    public void Detect_ShouldIgnoreRise_OutsideMaxWindow()
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
    public void Detect_ShouldFindMultipleNonOverlappingEvents()
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

    [Fact]
    public void Detect_AfterEpisode_ShouldNotEmitOverlap()
    {
        // Po wykryciu epizodu kontynuujemy od końca epizodu — żeby pojedyncze podlanie
        // nie wygenerowało dziesiątek nakładających się "skoków".
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(2), 35m),
            new(T0.AddMinutes(4), 38m),
            new(T0.AddMinutes(6), 40m),
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m);
        Assert.Single(events);
    }

    [Fact]
    public void Detect_ShouldRestart_AfterDryingBackBelowOriginalMin()
    {
        // 20 → spada do 18 → rośnie do 30. Minimum okna powinno przesunąć się na 18,
        // a delta = 12 nadal ≥ 10, więc 1 epizod, ale liczony od 18.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(5), 18m),
            new(T0.AddMinutes(15), 30m),
        };

        var events = WateringDetector.Detect(samples, minDelta: 10m);

        Assert.Single(events);
        Assert.Equal(18m, events[0].MoistureBefore);
        Assert.Equal(12m, events[0].DeltaMoisture);
    }

    [Fact]
    public void Detect_ShouldReturnEmpty_WhenMinDeltaNonPositive()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(5), 50m)
        };

        Assert.Empty(WateringDetector.Detect(samples, minDelta: 0m));
        Assert.Empty(WateringDetector.Detect(samples, minDelta: -1m));
    }

    [Fact]
    public void Detect_ShouldReturnEmpty_ForZeroOrNegativeWindow()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(5), 50m)
        };

        Assert.Empty(WateringDetector.Detect(samples, minDelta: 10m, maxWindow: TimeSpan.Zero));
    }

    [Fact]
    public void Detect_ShouldIgnoreSmallOscillations_BelowMinDelta()
    {
        // Czujnik glebowy ma typowy szum ±2-3% — to NIE jest podlanie.
        // Algorytm musi to zignorować, w przeciwnym razie pulpit byłby zalany fałszywymi podlaniami.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 30m),
            new(T0.AddMinutes(1), 31m),
            new(T0.AddMinutes(2), 29m),
            new(T0.AddMinutes(3), 32m),
            new(T0.AddMinutes(4), 30m),
            new(T0.AddMinutes(5), 33m),
        };

        var events = WateringDetector.Detect(samples, minDelta: 5m);

        Assert.Empty(events);
    }

    [Fact]
    public void Detect_ShouldHandleEmptySamples()
    {
        Assert.Empty(WateringDetector.Detect(Array.Empty<TimestampedMoisture>()));
    }

    [Fact]
    public void Detect_ShouldNotEmit_WhenAllSamplesIdentical()
    {
        // Steady-state: brak ruchu = brak podlania.
        var samples = Enumerable.Range(0, 20)
            .Select(i => new TimestampedMoisture(T0.AddMinutes(i), 45m))
            .ToList();

        Assert.Empty(WateringDetector.Detect(samples, minDelta: 5m));
    }

    [Fact]
    public void Detect_ShouldDetect_ExactlyAtMinDeltaBoundary()
    {
        // Granica włączna: dokładnie minDelta = trigger (>= w kodzie).
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(5), 25m),
        };

        var events = WateringDetector.Detect(samples, minDelta: 5m);
        Assert.Single(events);
        Assert.Equal(5m, events[0].DeltaMoisture);
    }

    [Fact]
    public void Detect_ShouldNotDetect_JustBelowMinDeltaBoundary()
    {
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 20m),
            new(T0.AddMinutes(5), 24.99m),
        };

        Assert.Empty(WateringDetector.Detect(samples, minDelta: 5m));
    }

    [Fact]
    public void Detect_ShouldHandleDescendingSeries_AsNoEvent()
    {
        // Spadek wilgotności przez cały okres = zwykłe wysychanie, NIE podlanie.
        var samples = new List<TimestampedMoisture>
        {
            new(T0, 60m),
            new(T0.AddMinutes(5), 55m),
            new(T0.AddMinutes(10), 50m),
            new(T0.AddMinutes(15), 45m),
        };

        Assert.Empty(WateringDetector.Detect(samples, minDelta: 5m));
    }
}
