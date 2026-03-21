namespace Greenhouse.Application.Charts;

/// <param name="SensorIdentifier">Stabilny identyfikator z odczytu (zwykle = <c>Sensor.ExternalId</c>, IEEE) — nie zależy od friendly name w topicu.</param>
public sealed record MoistureSeriesPointDto(
    DateTime UtcTime,
    string SensorIdentifier,
    Guid? SensorId,
    decimal? SoilMoisture,
    decimal? Temperature,
    int? Battery,
    int? LinkQuality);
