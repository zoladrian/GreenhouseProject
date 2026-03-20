using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Ingestion;

public sealed class MqttMessageIngestionService : IMqttMessageIngestionService
{
    private const string SensorTopicPrefix = "zigbee2mqtt/";
    private const string BridgeTopicPrefix = "zigbee2mqtt/bridge/";

    private readonly IMqttPayloadParser _payloadParser;
    private readonly ISensorReadingRepository _readingRepository;
    private readonly ISensorProvisioningService _sensorProvisioning;

    public MqttMessageIngestionService(
        IMqttPayloadParser payloadParser,
        ISensorReadingRepository readingRepository,
        ISensorProvisioningService sensorProvisioning)
    {
        _payloadParser = payloadParser;
        _readingRepository = readingRepository;
        _sensorProvisioning = sensorProvisioning;
    }

    public async Task IngestAsync(IncomingMqttMessage message, CancellationToken cancellationToken)
    {
        if (!TryGetSensorIdentifier(message.Topic, out var sensorIdentifier))
        {
            return;
        }

        var parsed = _payloadParser.ParseSensorPayload(message.Payload);

        var sensorId = await _sensorProvisioning.EnsureSensorAsync(sensorIdentifier, cancellationToken);

        var reading = SensorReading.Create(
            sensorIdentifier,
            message.ReceivedAtUtc,
            message.Topic,
            message.Payload,
            parsed.SoilMoisture,
            parsed.Temperature,
            parsed.Battery,
            parsed.LinkQuality,
            sensorId);

        await _readingRepository.AddAsync(reading, cancellationToken);
    }

    private static bool TryGetSensorIdentifier(string topic, out string sensorIdentifier)
    {
        sensorIdentifier = string.Empty;
        if (!topic.StartsWith(SensorTopicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (topic.StartsWith(BridgeTopicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Tylko zigbee2mqtt/<jedna_nazwa> — pełny JSON stanu. Pomija .../availability, .../set, tryb „attribute”.
        var remainder = topic.AsSpan(SensorTopicPrefix.Length).Trim();
        if (remainder.IsEmpty || remainder.Contains('/'))
        {
            return false;
        }

        sensorIdentifier = remainder.ToString();
        return !string.IsNullOrWhiteSpace(sensorIdentifier);
    }
}
