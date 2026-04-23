namespace Greenhouse.Application.Weather;

public sealed class WeatherInterpretationOptions
{
    public const string SectionName = "WeatherInterpretation";

    public decimal DewIntensityMax { get; init; } = 8m;
    public decimal LightRainIntensityMin { get; init; } = 12m;
    public decimal ModerateRainIntensityMin { get; init; } = 35m;
    public decimal HeavyRainIntensityMin { get; init; } = 70m;
    public decimal NightLightRawMax { get; init; } = 60m;
    public decimal DarkOrOvercastRawMax { get; init; } = 280m;
    public decimal BrightRawMax { get; init; } = 700m;
    public decimal SunnyRawMax { get; init; } = 1200m;
    public int DewMaxMinutes { get; init; } = 35;
}
