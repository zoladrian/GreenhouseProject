using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Infrastructure.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greenhouse.Infrastructure.IntegrationTests.Hosting;

/// <summary>
/// Sprawdza, że hosted service:
/// 1. Wywołuje merger raz przy braku błędów (po opóźnieniu startu).
/// 2. Przy wyjątku stosuje retry/backoff i nie zatrzymuje całego hosta.
/// 3. Po anulowaniu (host stop) szybko kończy bez wyciekających zadań.
///
/// Używa internal-konstruktora z krótkimi delay'ami, żeby cały test trwał setki ms.
/// </summary>
public sealed class SensorDuplicateCleanupHostedServiceTests
{
    private static readonly TimeSpan FastFirstRun = TimeSpan.FromMilliseconds(50);
    private static readonly IReadOnlyList<TimeSpan> FastBackoff = new[]
    {
        TimeSpan.FromMilliseconds(60),
        TimeSpan.FromMilliseconds(120),
        TimeSpan.FromMilliseconds(240),
    };

    private sealed class FakeMerger : ISensorDuplicateMerger
    {
        private readonly Func<int, Task<SensorDuplicateMergeResult>> _impl;
        public int Calls;

        public FakeMerger(Func<int, Task<SensorDuplicateMergeResult>> impl)
        {
            _impl = impl;
        }

        public async Task<SensorDuplicateMergeResult> MergeAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return await _impl(Calls);
        }
    }

    private sealed class FakeReadingRepo : ISensorReadingRepository
    {
        public int AlignCalls;

        public Task AddAsync(SensorReading reading, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SensorReading>>(Array.Empty<SensorReading>());

        public Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(
            IReadOnlyList<Guid> sensorIds, DateTime from, DateTime to, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SensorReading>>(Array.Empty<SensorReading>());

        public Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(
            IReadOnlyList<Guid> sensorIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SensorReading>>(Array.Empty<SensorReading>());

        public Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);

        public Task<int> AlignSensorIdentifierForSensorAsync(Guid sensorId, string externalId, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken cancellationToken)
        {
            AlignCalls++;
            return Task.FromResult(0);
        }

        public Task<bool> ExistsDuplicateAsync(string sensorIdentifier, DateTime receivedAtUtc, string topic, string rawPayloadJson, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }

    private static SensorDuplicateCleanupHostedService BuildSvc(FakeMerger merger, FakeReadingRepo repo)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISensorDuplicateMerger>(merger);
        services.AddSingleton<ISensorReadingRepository>(repo);
        var sp = services.BuildServiceProvider();
        return new SensorDuplicateCleanupHostedService(
            new ScopeFactoryAdapter(sp),
            NullLogger<SensorDuplicateCleanupHostedService>.Instance,
            FastFirstRun,
            FastBackoff);
    }

    [Fact]
    public async Task RunOnce_WhenMergerSucceeds_CallsAlignAndStops()
    {
        var merger = new FakeMerger(_ => Task.FromResult(new SensorDuplicateMergeResult(0, 0)));
        var repo = new FakeReadingRepo();
        var svc = BuildSvc(merger, repo);

        using var cts = new CancellationTokenSource();
        await svc.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await cts.CancelAsync();
        await svc.StopAsync(CancellationToken.None);

        Assert.Equal(1, merger.Calls);
        Assert.Equal(1, repo.AlignCalls);
    }

    [Fact]
    public async Task RunOnce_WhenCancelledQuickly_DoesNotInvokeMerger()
    {
        var merger = new FakeMerger(_ => Task.FromResult(new SensorDuplicateMergeResult(0, 0)));
        var repo = new FakeReadingRepo();
        // Pierwszy delay > niż timeout cancellation — anulujemy zanim merger w ogóle pójdzie.
        var services = new ServiceCollection();
        services.AddSingleton<ISensorDuplicateMerger>(merger);
        services.AddSingleton<ISensorReadingRepository>(repo);
        var sp = services.BuildServiceProvider();
        var svc = new SensorDuplicateCleanupHostedService(
            new ScopeFactoryAdapter(sp),
            NullLogger<SensorDuplicateCleanupHostedService>.Instance,
            firstRunDelay: TimeSpan.FromSeconds(2),
            retryBackoff: FastBackoff);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await svc.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await svc.StopAsync(CancellationToken.None);

        Assert.Equal(0, merger.Calls);
    }

    [Fact]
    public async Task RunOnce_WhenMergerThrowsOnce_RetriesAndSucceeds()
    {
        var merger = new FakeMerger(call =>
        {
            if (call == 1) throw new InvalidOperationException("boom");
            return Task.FromResult(new SensorDuplicateMergeResult(1, 0));
        });
        var repo = new FakeReadingRepo();
        var svc = BuildSvc(merger, repo);

        using var cts = new CancellationTokenSource();
        await svc.StartAsync(cts.Token);
        // FirstRun 50 ms + backoff 60 ms + drugie wykonanie — czekamy ~400 ms na pewno.
        await Task.Delay(TimeSpan.FromMilliseconds(400));
        await cts.CancelAsync();
        await svc.StopAsync(CancellationToken.None);

        Assert.True(merger.Calls >= 2, $"oczekiwano >= 2 wywołań mergera, było {merger.Calls}");
        Assert.True(repo.AlignCalls >= 1);
    }

    private sealed class ScopeFactoryAdapter : IServiceScopeFactory
    {
        private readonly IServiceProvider _root;
        public ScopeFactoryAdapter(IServiceProvider root) => _root = root;
        public IServiceScope CreateScope() => new Scope(_root);

        private sealed class Scope : IServiceScope
        {
            public Scope(IServiceProvider provider) => ServiceProvider = provider;
            public IServiceProvider ServiceProvider { get; }
            public void Dispose() { }
        }
    }
}
