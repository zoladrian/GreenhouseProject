namespace Greenhouse.Application.Abstractions;

/// <param name="IeeeAddress">Adres IEEE z JSON (Zigbee2MQTT), znormalizowany w parserze — stabilny klucz niezależny od friendly name w topicu.</param>
public sealed record ParsedSensorPayload(
    decimal? SoilMoisture,
    decimal? Temperature,
    int? Battery,
    int? LinkQuality,
    string? IeeeAddress = null);
