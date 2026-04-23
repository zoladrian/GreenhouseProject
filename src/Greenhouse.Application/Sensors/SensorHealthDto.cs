namespace Greenhouse.Application.Sensors;

public sealed record SensorHealthDto(
    Guid SensorId,
    string ExternalId,
    string? DisplayName,
    string Kind,
    Guid? NawaId,
    int? Battery,
    int? LinkQuality,
    bool? CleaningReminder,
    bool? Rain,
    decimal? RainIntensityRaw,
    decimal? IlluminanceRaw,
    DateTime? LastReadingUtc,
    int TotalReadings24h);
