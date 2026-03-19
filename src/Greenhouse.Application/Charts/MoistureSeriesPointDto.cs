namespace Greenhouse.Application.Charts;

public sealed record MoistureSeriesPointDto(
    DateTime UtcTime,
    string SensorIdentifier,
    Guid? SensorId,
    decimal? SoilMoisture,
    decimal? Temperature,
    int? Battery,
    int? LinkQuality);
