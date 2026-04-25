using Greenhouse.Application.Weather;
using Greenhouse.Domain.SensorReadings;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Tests.Weather;

public sealed class WeatherInterpretationServiceTests
{
    private readonly WeatherInterpretationService _sut = new(
        Options.Create(new WeatherInterpretationOptions()));

    [Fact]
    public void Interpret_ShouldClassifyDew_WhenLowIntensityAtNightAndShortSignal()
    {
        var now = DateTime.UtcNow;
        var history = new[]
        {
            MakeReading(now.AddMinutes(-10), rain: true, intensity: 4m, illuminance: 30m),
            MakeReading(now.AddMinutes(-4), rain: true, intensity: 5m, illuminance: 20m),
            MakeReading(now.AddMinutes(-1), rain: true, intensity: 4m, illuminance: 15m),
        };

        var result = _sut.Interpret(history[^1], history, now);

        Assert.Equal(RainLevel.DewOrFog, result.RainLevel);
        Assert.Equal(LightLevel.Night, result.LightLevel);
    }

    [Fact]
    public void Interpret_ShouldClassifyModerateRain_WhenIntensityCrossesThreshold()
    {
        var now = DateTime.UtcNow;
        var history = new[]
        {
            MakeReading(now.AddMinutes(-20), rain: true, intensity: 20m, illuminance: 500m),
            MakeReading(now.AddMinutes(-10), rain: true, intensity: 38m, illuminance: 510m),
        };

        var result = _sut.Interpret(history[^1], history, now);

        Assert.Equal(RainLevel.ModerateRain, result.RainLevel);
        Assert.Equal(LightLevel.Bright, result.LightLevel);
    }

    [Fact]
    public void Interpret_ShouldClassifyNoRain_WhenRainFlagFalseAndLowIntensity()
    {
        var now = DateTime.UtcNow;
        var latest = MakeReading(now.AddMinutes(-2), rain: false, intensity: 0m, illuminance: 300m);

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(RainLevel.NoRain, result.RainLevel);
    }

    [Fact]
    public void Interpret_ShouldClassifyHeavyRain_WhenIntensityAtThreshold()
    {
        // Granica włączna: intensity == HeavyRainIntensityMin (70).
        var now = DateTime.UtcNow;
        var latest = MakeReading(now, rain: true, intensity: 70m, illuminance: 800m);

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(RainLevel.HeavyRain, result.RainLevel);
    }

    [Fact]
    public void Interpret_ShouldClassifyModerateRain_JustBelowHeavyThreshold()
    {
        var now = DateTime.UtcNow;
        var latest = MakeReading(now, rain: true, intensity: 69.99m, illuminance: 800m);

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(RainLevel.ModerateRain, result.RainLevel);
    }

    [Fact]
    public void Interpret_ShouldClassifyLightRain_OnlyByRainFlag_WhenIntensityBelowLightMin()
    {
        // Specjalny wariant: rain=true ale intensity < LightRainIntensityMin (12). W dziennym świetle
        // (LightLevel.Bright lub wyższa) nie kwalifikuje się jako rosa, więc zostaje LightRain.
        var now = DateTime.UtcNow;
        var latest = MakeReading(now, rain: true, intensity: 9m, illuminance: 600m);

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(RainLevel.LightRain, result.RainLevel);
        Assert.Equal(LightLevel.Bright, result.LightLevel);
    }

    [Fact]
    public void Interpret_ShouldNotClassifyDew_WhenRainSignalExceedsDewMaxMinutes()
    {
        // rain=true z intensywnością ~rosy, ale TRWA dłużej niż DewMaxMinutes (35) → LightRain.
        var now = DateTime.UtcNow;
        var history = Enumerable.Range(0, 50)
            .Select(i => MakeReading(now.AddMinutes(-50 + i), rain: true, intensity: 5m, illuminance: 30m))
            .ToList();

        var result = _sut.Interpret(history[^1], history, now);

        Assert.Equal(RainLevel.LightRain, result.RainLevel);
        Assert.True(result.RainSignalMinutes >= 35);
    }

    [Fact]
    public void Interpret_ShouldNotClassifyDew_AtDayLight_EvenIfShortAndLowIntensity()
    {
        // Rosa wymaga ciemności (Night/DarkOrOvercast) — w jasnym świetle = LightRain.
        var now = DateTime.UtcNow;
        var latest = MakeReading(now, rain: true, intensity: 5m, illuminance: 600m);

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(LightLevel.Bright, result.LightLevel);
        Assert.Equal(RainLevel.LightRain, result.RainLevel);
    }

    [Theory]
    [InlineData(0, LightLevel.Night)]
    [InlineData(60, LightLevel.Night)]              // granica włączna NightLightRawMax
    [InlineData(61, LightLevel.DarkOrOvercast)]
    [InlineData(280, LightLevel.DarkOrOvercast)]    // granica włączna DarkOrOvercastRawMax
    [InlineData(281, LightLevel.Bright)]
    [InlineData(700, LightLevel.Bright)]            // granica włączna BrightRawMax
    [InlineData(701, LightLevel.Sunny)]
    [InlineData(1200, LightLevel.Sunny)]            // granica włączna SunnyRawMax
    [InlineData(1201, LightLevel.FullSun)]
    [InlineData(50000, LightLevel.FullSun)]
    public void Interpret_ShouldClassifyLightLevel_AcrossBoundaries(int illuminance, LightLevel expected)
    {
        var now = DateTime.UtcNow;
        var latest = MakeReading(now, rain: false, intensity: 0m, illuminance: illuminance);

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(expected, result.LightLevel);
    }

    [Fact]
    public void Interpret_ShouldFallBackToAverage20Min_WhenIlluminanceRawIsNull()
    {
        // Niektóre urządzenia raportują tylko średnią — Interpret musi to obsłużyć
        // (klasyfikacja świetlna wciąż musi działać).
        var now = DateTime.UtcNow;
        var latest = SensorReading.Create(
            sensorIdentifier: "rain-1",
            receivedAtUtc: now,
            topic: "zigbee2mqtt/rain-1",
            rawPayloadJson: "{}",
            soilMoisture: null,
            temperature: null,
            battery: 95,
            linkQuality: 160,
            rain: false,
            rainIntensityRaw: 0m,
            illuminanceRaw: null,
            illuminanceAverage20MinRaw: 800m,
            illuminanceMaximumTodayRaw: null,
            cleaningReminder: false,
            sensorId: Guid.NewGuid());

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(LightLevel.Sunny, result.LightLevel);
    }

    [Fact]
    public void ComputeContinuousRainMinutes_ShouldStop_AtFirstNonRainEntry()
    {
        // Dwa epizody deszczu rozdzielone przerwą. Sygnał ciągły = tylko ostatni segment.
        var now = DateTime.UtcNow;
        var history = new[]
        {
            MakeReading(now.AddMinutes(-60), rain: true, intensity: 30m, illuminance: 500m),
            MakeReading(now.AddMinutes(-50), rain: false, intensity: 0m, illuminance: 600m),
            MakeReading(now.AddMinutes(-30), rain: true, intensity: 15m, illuminance: 400m),
            MakeReading(now.AddMinutes(-10), rain: true, intensity: 20m, illuminance: 350m),
        };

        var result = _sut.Interpret(history[^1], history, now);

        Assert.Equal(20, result.RainSignalMinutes); // -30 → -10 = 20 min
    }

    [Fact]
    public void Interpret_ShouldHandleNullableSensorFields_Gracefully()
    {
        // Nowy/uszkodzony czujnik: brak rain, intensity, illuminance.
        var now = DateTime.UtcNow;
        var latest = SensorReading.Create(
            sensorIdentifier: "rain-broken",
            receivedAtUtc: now,
            topic: "zigbee2mqtt/rain-broken",
            rawPayloadJson: "{}",
            soilMoisture: null,
            temperature: null,
            battery: null,
            linkQuality: null,
            rain: null,
            rainIntensityRaw: null,
            illuminanceRaw: null,
            illuminanceAverage20MinRaw: null,
            illuminanceMaximumTodayRaw: null,
            cleaningReminder: null,
            sensorId: Guid.NewGuid());

        var result = _sut.Interpret(latest, [latest], now);

        Assert.Equal(RainLevel.NoRain, result.RainLevel);
        Assert.Equal(LightLevel.Night, result.LightLevel);
    }

    private static SensorReading MakeReading(DateTime at, bool rain, decimal intensity, decimal illuminance)
        => SensorReading.Create(
            sensorIdentifier: "rain-1",
            receivedAtUtc: at,
            topic: "zigbee2mqtt/rain-1",
            rawPayloadJson: "{}",
            soilMoisture: null,
            temperature: null,
            battery: 95,
            linkQuality: 160,
            rain: rain,
            rainIntensityRaw: intensity,
            illuminanceRaw: illuminance,
            illuminanceAverage20MinRaw: null,
            illuminanceMaximumTodayRaw: null,
            cleaningReminder: false,
            sensorId: Guid.NewGuid());
}
