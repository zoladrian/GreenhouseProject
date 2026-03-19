using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Ingestion;

public sealed class MqttMessageIngestionService : IMqttMessageIngestionService
{
    private const string SensorTopicPrefix = "zigbee2mqtt/";
    private const string BridgeTopicPrefix = "zigbee2mqtt/bridge/";

    private readonly IMqttPayloadParser _payloadParser;
    private readonly ISensorReadingRepository _readingRepository;

    public MqttMessageIngestionService(
        IMqttPayloadParser payloadParser,
        ISensorReadingRepository readingRepository)
    {
        _payloadParser = payloadParser;
        _readingRepository = readingRepository;
    }

    public async Task IngestAsync(IncomingMqttMessage message, CancellationToken cancellationToken)
    {
        if (!TryGetSensorIdentifier(message.Topic, out var sensorIdentifier))
        {
            return;
        }

        var parsed = _payloadParser.ParseSensorPayload(message.Payload);

        var reading = SensorReading.Create(
            sensorIdentifier,
            message.ReceivedAtUtc,
            message.Topic,
            message.Payload,
            parsed.SoilMoisture,
            parsed.Temperature,
            parsed.Battery,
            parsed.LinkQuality);

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

        sensorIdentifier = topic[SensorTopicPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(sensorIdentifier);
    }
}
