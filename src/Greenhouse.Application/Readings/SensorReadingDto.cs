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
    int? LinkQuality);
