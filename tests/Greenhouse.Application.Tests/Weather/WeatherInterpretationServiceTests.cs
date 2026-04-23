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
