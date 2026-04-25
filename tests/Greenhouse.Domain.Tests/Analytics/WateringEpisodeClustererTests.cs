using Greenhouse.Domain.Analytics;

namespace Greenhouse.Domain.Tests.Analytics;

public sealed class WateringEpisodeClustererTests
{
    private static readonly DateTime T0 = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid S1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid S2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid S3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Cluster_SingleSensor_ShouldBeLikelyManual()
    {
        var e = new WateringEvent(T0.AddMinutes(5), 20m, 40m, 20m, TimeSpan.FromMinutes(5));
        var episodes = WateringEpisodeClusterer.Cluster([(S1, e)]);

        Assert.Single(episodes);
        Assert.Equal(WateringEventInferredKind.LikelyManual, episodes[0].InferredKind);
        Assert.Equal(1, episodes[0].ContributingSensorCount);
    }

    [Fact]
    public void Cluster_TwoSensorsCloseInTime_ShouldBeLikelyRain()
    {
        var e1 = new WateringEvent(T0, 22m, 48m, 26m, TimeSpan.FromMinutes(5));
        var e2 = new WateringEvent(T0.AddMinutes(15), 25m, 50m, 25m, TimeSpan.FromMinutes(5));
        var episodes = WateringEpisodeClusterer.Cluster([(S1, e1), (S2, e2)]);

        Assert.Single(episodes);
        Assert.Equal(WateringEventInferredKind.LikelyRain, episodes[0].InferredKind);
        Assert.Equal(2, episodes[0].ContributingSensorCount);
    }

    [Fact]
    public void Cluster_TwoSensorsFarApart_ShouldBeTwoManualEpisodes()
    {
        var e1 = new WateringEvent(T0, 20m, 40m, 20m, TimeSpan.FromMinutes(5));
        var e2 = new WateringEvent(T0.AddHours(3), 20m, 38m, 18m, TimeSpan.FromMinutes(5));
        var episodes = WateringEpisodeClusterer.Cluster([(S1, e1), (S2, e2)]);

        Assert.Equal(2, episodes.Count);
        Assert.All(episodes, ep => Assert.Equal(WateringEventInferredKind.LikelyManual, ep.InferredKind));
    }

    [Fact]
    public void Cluster_ShouldNot_ChainLink_WhenTotalSpanExceedsLink()
    {
        // 0 → 40 → 80 min, link=45 min. Stary algorytm (union-find po wszystkich parach
        // z dt ≤ link) zrobiłby 1 klaster z całością (bo 0-40 i 40-80 są w linku, więc
        // tranzytywnie też 0-80). Nowy algorytm (kotwica = pierwsze) zrobi:
        // - klaster A: 0 i 40 min (anchor=0, dt=40 ≤ 45)
        // - klaster B: 80 min (dt od 0 = 80 > 45)
        var link = TimeSpan.FromMinutes(45);
        var e1 = new WateringEvent(T0, 20m, 35m, 15m, TimeSpan.FromMinutes(5));
        var e2 = new WateringEvent(T0.AddMinutes(40), 25m, 40m, 15m, TimeSpan.FromMinutes(5));
        var e3 = new WateringEvent(T0.AddMinutes(80), 30m, 45m, 15m, TimeSpan.FromMinutes(5));
        var episodes = WateringEpisodeClusterer.Cluster([(S1, e1), (S2, e2), (S3, e3)], link);

        Assert.Equal(2, episodes.Count);
        Assert.Equal(2, episodes[0].ContributingSensorCount);
        Assert.Equal(1, episodes[1].ContributingSensorCount);
    }

    [Fact]
    public void Cluster_ShouldReturnEmpty_ForEmptyInput()
    {
        Assert.Empty(WateringEpisodeClusterer.Cluster(Array.Empty<(Guid, WateringEvent)>()));
    }

    [Fact]
    public void Cluster_ShouldOrderEpisodesByTime()
    {
        var e1 = new WateringEvent(T0.AddHours(5), 20m, 40m, 20m, TimeSpan.FromMinutes(5));
        var e2 = new WateringEvent(T0, 25m, 50m, 25m, TimeSpan.FromMinutes(5));
        var episodes = WateringEpisodeClusterer.Cluster([(S1, e1), (S2, e2)]);

        Assert.Equal(2, episodes.Count);
        Assert.True(episodes[0].DetectedAtUtc < episodes[1].DetectedAtUtc);
    }
}
