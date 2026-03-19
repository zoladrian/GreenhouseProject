namespace Greenhouse.Application.Sensors;

public sealed record SensorHealthDto(
    Guid SensorId,
    string ExternalId,
    string? DisplayName,
    Guid? NawaId,
    int? Battery,
    int? LinkQuality,
    DateTime? LastReadingUtc,
    int TotalReadings24h);
