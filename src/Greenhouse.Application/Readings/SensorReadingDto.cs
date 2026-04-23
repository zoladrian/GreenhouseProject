namespace Greenhouse.Application.Readings;

public sealed record SensorReadingDto(
    Guid Id,
    string SensorIdentifier,
    DateTime ReceivedAtUtc,
    string Topic,
    string RawPayloadJson,
    decimal? SoilMoisture,
    decimal? Temperature,
    int? Battery,
    int? LinkQuality,
    bool? Rain,
    decimal? RainIntensityRaw,
    decimal? IlluminanceRaw,
    decimal? IlluminanceAverage20MinRaw,
    decimal? IlluminanceMaximumTodayRaw,
    bool? CleaningReminder);
