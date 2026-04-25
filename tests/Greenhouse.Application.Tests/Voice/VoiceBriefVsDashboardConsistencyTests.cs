using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Time;
using Greenhouse.Application.Voice;
using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Domain.Sensors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Tests.Voice;

/// <summary>
/// Regresja: TTS i dashboard muszą używać tej samej polityki <c>oldestReadingUtc</c>.
/// Kiedyś brief liczył najstarszy odczyt z latestReadings (w tym battery-only),
/// a dashboard tylko z odczytów z SoilMoisture. Skutek: różny <see cref="OperatorStatus"/>
/// dla tej samej nawy w głosie i na pulpicie.
/// </summary>
public sealed class VoiceBriefVsDashboardConsistencyTests
{
    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task BriefAndDashboard_ReportSameStatus_WhenLatestSoilIsStaleButBatteryIsFresh()
    {
        // Aranż: świeży odczyt baterii + bardzo stary odczyt z wilgotnością.
        // Oczekiwanie: status = NoData w obu serwisach (próg 60 min minął wzgl. najstarszego soil).
        var clock = new FakeClock { UtcNow = new DateTime(2026, 4, 24, 14, 0, 0, DateTimeKind.Utc) };

        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Nawa Mix", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var soil = Sensor.Register("soil-mix", SensorKind.Soil);
        soil.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(soil, CancellationToken.None);

        // Stary odczyt z wilgotnością (poza progiem świeżości 60 min).
        readingRepo.Add(SensorReading.Create("soil-mix", clock.UtcNow.AddHours(-3), "t", "{\"battery\":91}", 50m, 21m, 91, 100, soil.Id));
        // Świeży odczyt tylko z baterią (bez SoilMoisture).
        readingRepo.Add(SensorReading.Create("soil-mix", clock.UtcNow.AddMinutes(-2), "t", "{\"battery\":90}", null, null, 90, 100, soil.Id));

        var dashboard = BuildDashboardSut(nawaRepo, sensorRepo, readingRepo, clock);
        var brief = BuildBriefSut(nawaRepo, sensorRepo, readingRepo, clock);

        var dashboardResult = await dashboard.ExecuteAsync(CancellationToken.None);
        var briefResult = await brief.ExecuteAsync(nawa.Id, CancellationToken.None);

        Assert.Single(dashboardResult);
        Assert.Equal(OperatorStatus.NoData, dashboardResult[0].Status);
        Assert.NotNull(briefResult);
        // Brief jest tekstem, ale jego treść statusu pochodzi z OperatorStatusCalculator —
        // jeśli kontrakt jest zachowany, mówi „brak danych lub dane są zbyt stare”.
        Assert.Contains("brak danych", briefResult!.SpokenText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BriefAndDashboard_AgreeOnOk_WhenSoilIsFresh()
    {
        var clock = new FakeClock { UtcNow = new DateTime(2026, 4, 24, 14, 0, 0, DateTimeKind.Utc) };

        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Nawa Ok", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var soil = Sensor.Register("soil-ok", SensorKind.Soil);
        soil.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(soil, CancellationToken.None);

        readingRepo.Add(SensorReading.Create("soil-ok", clock.UtcNow.AddMinutes(-5), "t", "{}", 55m, 21m, 90, 100, soil.Id));

        var dashboard = BuildDashboardSut(nawaRepo, sensorRepo, readingRepo, clock);
        var brief = BuildBriefSut(nawaRepo, sensorRepo, readingRepo, clock);

        var d = await dashboard.ExecuteAsync(CancellationToken.None);
        var b = await brief.ExecuteAsync(nawa.Id, CancellationToken.None);

        Assert.Equal(OperatorStatus.Ok, d[0].Status);
        Assert.NotNull(b);
        Assert.Contains("w normie", b!.SpokenText, StringComparison.OrdinalIgnoreCase);
    }

    private static GetDashboardQueryService BuildDashboardSut(
        InMemoryNawaRepo nawaRepo,
        InMemorySensorRepo sensorRepo,
        InMemoryReadingRepo readingRepo,
        FakeClock clock)
    {
        var voice = Options.Create(new VoiceOptions());
        var analytics = Options.Create(new AnalyticsOptions());
        var watering = new GetWateringEventsQueryService(sensorRepo, readingRepo);
        var tz = new GreenhouseTimeZoneResolver(NullLogger<GreenhouseTimeZoneResolver>.Instance);
        return new GetDashboardQueryService(
            nawaRepo,
            sensorRepo,
            readingRepo,
            NullLogger<GetDashboardQueryService>.Instance,
            voice,
            analytics,
            watering,
            clock,
            tz);
    }

    private static GetNawaVoiceBriefQueryService BuildBriefSut(
        InMemoryNawaRepo nawaRepo,
        InMemorySensorRepo sensorRepo,
        InMemoryReadingRepo readingRepo,
        FakeClock clock)
    {
        var voice = Options.Create(new VoiceOptions());
        var analytics = Options.Create(new AnalyticsOptions());
        var watering = new GetWateringEventsQueryService(sensorRepo, readingRepo);
        var tz = new GreenhouseTimeZoneResolver(NullLogger<GreenhouseTimeZoneResolver>.Instance);
        return new GetNawaVoiceBriefQueryService(voice, analytics, nawaRepo, sensorRepo, readingRepo, watering, clock, tz);
    }

    private sealed class InMemoryNawaRepo : INawaRepository
    {
        private readonly List<Nawa> _items = [];
        public Task AddAsync(Nawa nawa, CancellationToken ct) { _items.Add(nawa); return Task.CompletedTask; }
        public Task<Nawa?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(n => n.Id == id));
        public Task<IReadOnlyList<Nawa>> ListAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<Nawa>)_items);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class InMemorySensorRepo : ISensorRepository
    {
        private readonly List<Sensor> _items = [];
        public Task AddAsync(Sensor sensor, CancellationToken ct) { _items.Add(sensor); return Task.CompletedTask; }
        public Task<Sensor?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(s => s.Id == id));
        public Task<Sensor?> GetByExternalIdAsync(string externalId, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(s => s.ExternalId == externalId));
        public Task<Sensor?> GetByExternalIdForUpdateAsync(string externalId, CancellationToken ct) => GetByExternalIdAsync(externalId, ct);
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var s = _items.FirstOrDefault(x => x.Id == id);
            if (s is null) return Task.FromResult(false);
            _items.Remove(s);
            return Task.FromResult(true);
        }
        public Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<Sensor>)_items);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class InMemoryReadingRepo : ISensorReadingRepository
    {
        private readonly List<SensorReading> _items = [];
        public void Add(SensorReading r) => _items.Add(r);
        public Task AddAsync(SensorReading reading, CancellationToken ct) { _items.Add(reading); return Task.CompletedTask; }
        public Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<SensorReading>)_items.OrderByDescending(r => r.ReceivedAtUtc).Take(count).ToList());
        public Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(IReadOnlyList<Guid> sensorIds, DateTime from, DateTime to, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<SensorReading>)_items
                .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value) && r.ReceivedAtUtc >= from && r.ReceivedAtUtc <= to)
                .OrderBy(r => r.ReceivedAtUtc).ToList());
        public Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(IReadOnlyList<Guid> sensorIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<SensorReading>)_items
                .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value))
                .GroupBy(r => r.SensorId).Select(g => g.OrderByDescending(r => r.ReceivedAtUtc).First()).ToList());
        public Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken ct) => Task.FromResult<string?>(null);
        public Task<int> AlignSensorIdentifierForSensorAsync(Guid sensorId, string externalId, CancellationToken ct) => Task.FromResult(0);
        public Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken ct) => Task.FromResult(0);
        public Task<bool> ExistsDuplicateAsync(string sensorIdentifier, DateTime receivedAtUtc, string topic, string rawPayloadJson, CancellationToken ct) => Task.FromResult(false);
    }
}
