namespace Greenhouse.Application.Weather;

public sealed record WeatherControlConfigDto(
    decimal RainDetectedMinRaw,
    decimal HighHumidityMinRaw,
    decimal SunnyMinRaw,
    decimal CloudyMaxRaw,
    string SunriseLocal,
    string SunsetLocal,
    string ManualRainStatus,
    string ManualLightStatus,
    DateTime UpdatedAtUtc);

public sealed record WeatherCurrentStatusDto(
    string RainStatus,
    string LightStatus,
    bool IsNightBySchedule,
    decimal? RainIntensityRaw,
    decimal? IlluminanceRaw,
    DateTime? SourceUtcTime);

public sealed record SunScheduleEntryDto(
    DateOnly Date,
    string SunriseLocal,
    string SunsetLocal);

public sealed record SunScheduleImportResultDto(
    int ImportedRows,
    int IgnoredRows);
