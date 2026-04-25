using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Time;
using Greenhouse.Application.Voice;
using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Domain.Sensors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Tests.Voice;

public sealed class GetVoiceDailyReportQueryServiceTests
{
    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 4, 25, 10, 0, 0, DateTimeKind.Utc);
    }

    private static GetVoiceDailyReportQueryService CreateSut(
        InMemoryNawaRepo nawy,
        InMemorySensorRepo sensors,
        InMemoryReadingRepo readings,
        FakeClock clock,
        VoiceOptions? voice = null,
        AnalyticsOptions? analytics = null)
    {
        var voiceOpt = Options.Create(voice ?? new VoiceOptions());
        var analyticsOpt = Options.Create(analytics ?? new AnalyticsOptions());
        var watering = new GetWateringEventsQueryService(sensors, readings);
        var tz = new GreenhouseTimeZoneResolver(NullLogger<GreenhouseTimeZoneResolver>.Instance);
        return new GetVoiceDailyReportQueryService(voiceOpt, analyticsOpt, nawy, sensors, readings, watering, clock, tz);
    }

    [Fact]
    public async Task Execute_ShouldReturnLeadinAndLocalDate_FromClock_NotSystemTime()
    {
        // Determinizm: cała wypowiedź NIE może zależeć od DateTime.UtcNow w środowisku CI/Pi.
        var clock = new FakeClock { UtcNow = new DateTime(2026, 4, 25, 10, 0, 0, DateTimeKind.Utc) };
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock,
            voice: new VoiceOptions { GreetingLeadin = "Witaj", TimeZoneId = "Europe/Warsaw" });

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal("Witaj", dto.GreetingLeadin);
        // 10:00 UTC w Europe/Warsaw (kwiecień - CEST UTC+2) = 12:00 lokalnie.
        Assert.Equal("12:00", dto.LocalTime);
        Assert.Contains("kwietnia 2026", dto.LocalDateLong);
        Assert.Empty(dto.Nawy);
    }

    [Fact]
    public async Task Execute_ShouldOmitInactiveNawy_AndOrderByCreatedAt()
    {
        var clock = new FakeClock();
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var older = Nawa.Create("Pierwsza", null);
        var inactive = Nawa.Create("Wyłączona", null);
        inactive.SetActive(false);
        var newer = Nawa.Create("Druga", null);
        await nawaRepo.AddAsync(older, CancellationToken.None);
        await nawaRepo.AddAsync(inactive, CancellationToken.None);
        await nawaRepo.AddAsync(newer, CancellationToken.None);

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock);

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, dto.Nawy.Count);
        Assert.DoesNotContain(dto.Nawy, n => n.NawaName == "Wyłączona");
        Assert.Equal("Pierwsza", dto.Nawy[0].NawaName);
        Assert.Equal(1, dto.Nawy[0].Order);
        Assert.Equal("Druga", dto.Nawy[1].NawaName);
        Assert.Equal(2, dto.Nawy[1].Order);
    }

    [Fact]
    public async Task Execute_ShouldReportNoSensors_WhenNawaHasNoneAssigned()
    {
        var clock = new FakeClock();
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Pusta", null);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock);

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        var line = Assert.Single(dto.Nawy);
        Assert.Equal(0, line.AssignedSensorCount);
        Assert.Equal(0, line.ReadingCount);
        Assert.Null(line.AvgSoilMoisture);
        Assert.Null(line.AvgTemperature);
        Assert.Contains("brak przypisanych", line.MoistureAssessment);
        Assert.Contains("brak przypisanych", line.TemperatureAssessment);
    }

    [Fact]
    public async Task Execute_ShouldReportNoReadings_WhenSensorsAssignedButSilent()
    {
        var clock = new FakeClock();
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Cicha", null);
        nawa.UpdateMoistureThresholds(30m, 70m);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);
        var s1 = Sensor.Register("s-quiet");
        s1.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(s1, CancellationToken.None);

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock);

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        var line = Assert.Single(dto.Nawy);
        Assert.Equal(1, line.AssignedSensorCount);
        Assert.Equal(0, line.ReadingCount);
        Assert.Contains("brak zapisanych odczytów", line.MoistureAssessment);
    }

    [Fact]
    public async Task Execute_ShouldComputeAverages_FromMidnightLocal()
    {
        // O 10:00 UTC (12:00 CEST) bierzemy odczyty od 00:00 CEST (= 22:00 UTC dnia poprzedniego).
        // Odczyt sprzed 13h UTC (21:00 prev UTC = 23:00 prev CEST) NIE wlicza się do dzisiejszej średniej.
        var clock = new FakeClock { UtcNow = new DateTime(2026, 4, 25, 10, 0, 0, DateTimeKind.Utc) };
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Salon", null);
        nawa.UpdateMoistureThresholds(30m, 70m);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);
        var sensor = Sensor.Register("s-1");
        sensor.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(sensor, CancellationToken.None);

        readingRepo.Add(SensorReading.Create("s-1", clock.UtcNow.AddHours(-13), "t", "{}", 90m, 30m, 95, 100, sensor.Id));
        readingRepo.Add(SensorReading.Create("s-1", clock.UtcNow.AddHours(-1), "t", "{}", 50m, 20m, 95, 100, sensor.Id));
        readingRepo.Add(SensorReading.Create("s-1", clock.UtcNow.AddMinutes(-30), "t", "{}", 60m, 22m, 95, 100, sensor.Id));

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock);

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        var line = Assert.Single(dto.Nawy);
        Assert.Equal(2, line.ReadingCount);
        Assert.Equal(55m, line.AvgSoilMoisture);
        Assert.Equal(21m, line.AvgTemperature);
        Assert.Equal("Wilgotność poprawna.", line.MoistureAssessment);
    }

    [Fact]
    public async Task Execute_ShouldAppendDrySinceWateringClause_WhenAvgBelowMin_AndNoEventsInLookback()
    {
        // Średnia < MoistureMin → musi się pojawić sufiks z VoiceWateringSpeech.
        var clock = new FakeClock { UtcNow = new DateTime(2026, 4, 25, 10, 0, 0, DateTimeKind.Utc) };
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var nawa = Nawa.Create("Sucha", null);
        nawa.UpdateMoistureThresholds(50m, 80m);
        await nawaRepo.AddAsync(nawa, CancellationToken.None);
        var sensor = Sensor.Register("s-dry");
        sensor.AssignToNawa(nawa.Id);
        await sensorRepo.AddAsync(sensor, CancellationToken.None);

        readingRepo.Add(SensorReading.Create("s-dry", clock.UtcNow.AddMinutes(-30), "t", "{}", 25m, 22m, 95, 100, sensor.Id));

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock,
            analytics: new AnalyticsOptions { WateringLookbackDays = 7 });

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        var line = Assert.Single(dto.Nawy);
        Assert.Contains("anomalia wilgotności", line.MoistureAssessment);
        Assert.Contains("siedem dni", line.MoistureAssessment);
    }

    [Fact]
    public async Task Execute_ShouldFallback_WhenGreetingLeadinIsBlank()
    {
        var clock = new FakeClock();
        var nawaRepo = new InMemoryNawaRepo();
        var sensorRepo = new InMemorySensorRepo();
        var readingRepo = new InMemoryReadingRepo();

        var sut = CreateSut(nawaRepo, sensorRepo, readingRepo, clock,
            voice: new VoiceOptions { GreetingLeadin = "   ", TimeZoneId = "Europe/Warsaw" });

        var dto = await sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal("Dzień dobry", dto.GreetingLeadin);
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

        public Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken ct) =>
            Task.FromResult<string?>(null);

        public Task<int> AlignSensorIdentifierForSensorAsync(Guid sensorId, string externalId, CancellationToken ct) => Task.FromResult(0);
        public Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken ct) => Task.FromResult(0);

        public Task<bool> ExistsDuplicateAsync(string sensorIdentifier, DateTime receivedAtUtc, string topic, string rawPayloadJson, CancellationToken ct) =>
            Task.FromResult(false);
    }
}
