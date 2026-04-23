using Greenhouse.Application.Weather;

namespace Greenhouse.Application.Charts;

public sealed record WeatherSeriesPointDto(
    DateTime UtcTime,
    string SensorIdentifier,
    Guid? SensorId,
    bool? Rain,
    decimal? RainIntensityRaw,
    decimal? IlluminanceRaw,
    decimal? IlluminanceAverage20MinRaw,
    decimal? IlluminanceMaximumTodayRaw,
    int? Battery,
    int? LinkQuality,
    bool? CleaningReminder,
    RainLevel? RainLevel,
    LightLevel? LightLevel);
