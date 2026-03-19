namespace Greenhouse.Application.Abstractions;

public sealed record ParsedSensorPayload(
    decimal? SoilMoisture,
    decimal? Temperature,
    int? Battery,
    int? LinkQuality);
