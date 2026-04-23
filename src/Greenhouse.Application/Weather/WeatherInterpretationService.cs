using Greenhouse.Domain.SensorReadings;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Weather;

public sealed class WeatherInterpretationService
{
    private readonly WeatherInterpretationOptions _options;

    public WeatherInterpretationService(IOptions<WeatherInterpretationOptions> options)
    {
        _options = options.Value;
    }

    public WeatherInterpretationResult Interpret(
        SensorReading latest,
        IReadOnlyList<SensorReading> recentHistory,
        DateTime nowUtc)
    {
        var rainSignalMinutes = ComputeContinuousRainMinutes(latest, recentHistory, nowUtc);
        var lightLevel = ClassifyLight(latest);
        var rainLevel = ClassifyRain(latest, lightLevel, rainSignalMinutes);
        var reason = BuildReason(latest, lightLevel, rainSignalMinutes, rainLevel);
        return new WeatherInterpretationResult(rainLevel, lightLevel, rainSignalMinutes, reason);
    }

    private RainLevel ClassifyRain(SensorReading latest, LightLevel lightLevel, int rainSignalMinutes)
    {
        var rain = latest.Rain ?? false;
        var intensity = latest.RainIntensityRaw ?? 0m;

        if (!rain && intensity <= _options.DewIntensityMax)
        {
            return RainLevel.NoRain;
        }

        if (rain &&
            intensity <= _options.DewIntensityMax &&
            rainSignalMinutes <= _options.DewMaxMinutes &&
            lightLevel is LightLevel.Night or LightLevel.DarkOrOvercast)
        {
            return RainLevel.DewOrFog;
        }

        if (intensity >= _options.HeavyRainIntensityMin)
            return RainLevel.HeavyRain;
        if (intensity >= _options.ModerateRainIntensityMin)
            return RainLevel.ModerateRain;
        if (intensity >= _options.LightRainIntensityMin || rain)
            return RainLevel.LightRain;

        return RainLevel.NoRain;
    }

    private LightLevel ClassifyLight(SensorReading latest)
    {
        var raw = latest.IlluminanceRaw ?? latest.IlluminanceAverage20MinRaw ?? 0m;
        if (raw <= _options.NightLightRawMax)
            return LightLevel.Night;
        if (raw <= _options.DarkOrOvercastRawMax)
            return LightLevel.DarkOrOvercast;
        if (raw <= _options.BrightRawMax)
            return LightLevel.Bright;
        if (raw <= _options.SunnyRawMax)
            return LightLevel.Sunny;
        return LightLevel.FullSun;
    }

    private static int ComputeContinuousRainMinutes(
        SensorReading latest,
        IReadOnlyList<SensorReading> recentHistory,
        DateTime nowUtc)
    {
        if (latest.Rain != true)
            return 0;

        var ordered = recentHistory
            .Where(x => x.ReceivedAtUtc <= nowUtc)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .ToList();

        if (ordered.Count == 0)
            return 0;

        DateTime? oldestRain = null;
        foreach (var row in ordered)
        {
            if (row.Rain != true)
                break;
            oldestRain = row.ReceivedAtUtc;
        }

        if (!oldestRain.HasValue)
            return 0;

        return (int)Math.Round((latest.ReceivedAtUtc - oldestRain.Value).TotalMinutes);
    }

    private static string BuildReason(
        SensorReading latest,
        LightLevel lightLevel,
        int rainSignalMinutes,
        RainLevel rainLevel)
    {
        var intensity = latest.RainIntensityRaw?.ToString("0.##") ?? "brak";
        return $"rain={latest.Rain?.ToString() ?? "null"}, intensity={intensity}, light={lightLevel}, signalMin={rainSignalMinutes}, level={rainLevel}";
    }
}
