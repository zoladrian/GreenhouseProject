using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.SensorReadings;
using Microsoft.Extensions.Logging;

namespace Greenhouse.Application.Ingestion;

public sealed class MqttMessageIngestionService : IMqttMessageIngestionService
{
    private const string SensorTopicPrefix = "zigbee2mqtt/";
    private const string BridgeTopicPrefix = "zigbee2mqtt/bridge/";

    private readonly IMqttPayloadParser _payloadParser;
    private readonly ISensorReadingRepository _readingRepository;
    private readonly ISensorProvisioningService _sensorProvisioning;
    private readonly ILogger<MqttMessageIngestionService> _logger;
    private readonly IMqttIngestTelemetry _telemetry;

    public MqttMessageIngestionService(
        IMqttPayloadParser payloadParser,
        ISensorReadingRepository readingRepository,
        ISensorProvisioningService sensorProvisioning,
        ILogger<MqttMessageIngestionService> logger,
        IMqttIngestTelemetry telemetry)
    {
        _payloadParser = payloadParser;
        _readingRepository = readingRepository;
        _sensorProvisioning = sensorProvisioning;
        _logger = logger;
        _telemetry = telemetry;
    }

    public async Task IngestAsync(IncomingMqttMessage message, CancellationToken cancellationToken)
    {
        if (!TryGetSensorIdentifier(message.Topic, out var sensorIdentifier, out var skipReason))
        {
            _telemetry.NotifyIngestSkippedNonSensorTopic();
            _logger.LogTrace("MQTT pominięto topic={Topic}, powód={Reason}", message.Topic, skipReason);
            return;
        }

        var parsed = _payloadParser.ParseSensorPayload(message.Payload);

        var externalId = ZigbeeIeeeAddress.TryNormalize(parsed.IeeeAddress, out var ieeeNorm)
            ? ieeeNorm
            : sensorIdentifier;

        var hasMetric = parsed.SoilMoisture.HasValue || parsed.Temperature.HasValue || parsed.Battery.HasValue ||
                        parsed.LinkQuality.HasValue;
        var payloadTrim = message.Payload.TrimStart();
        if (!hasMetric && payloadTrim.StartsWith('{'))
        {
            _logger.LogDebug(
                "MQTT topic={Topic}, czujnik={Sensor}: JSON bez pól soil_moisture/temperature/battery/linkquality (tryb attribute w Z2M?). Fragment={Snippet}",
                message.Topic,
                externalId,
                Snippet(message.Payload, 160));
        }

        var ensured = await _sensorProvisioning.EnsureSensorAsync(
            new EnsureSensorInput(sensorIdentifier, externalId),
            cancellationToken);
        if (ensured.CreatedNew)
        {
            _logger.LogInformation(
                "Zarejestrowano nowy czujnik z MQTT: topic={TopicName}, klucz={CanonicalId}, SensorId={SensorId}",
                sensorIdentifier,
                externalId,
                ensured.SensorId);
        }

        var reading = SensorReading.Create(
            sensorIdentifier,
            message.ReceivedAtUtc,
            message.Topic,
            message.Payload,
            parsed.SoilMoisture,
            parsed.Temperature,
            parsed.Battery,
            parsed.LinkQuality,
            ensured.SensorId);

        await _readingRepository.AddAsync(reading, cancellationToken);
        _telemetry.NotifyReadingPersisted();

        _logger.LogTrace(
            "MQTT zapis odczytu ExternalId={ExternalId}, wilgotność={M}, temp={T}, bateria={B}, LQ={Lq}",
            externalId,
            parsed.SoilMoisture,
            parsed.Temperature,
            parsed.Battery,
            parsed.LinkQuality);
    }

    private static string Snippet(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "∅";
        }

        var t = text.Replace('\r', ' ').Replace('\n', ' ');
        return t.Length <= maxLen ? t : t[..maxLen] + "…";
    }

    private static bool TryGetSensorIdentifier(string topic, out string sensorIdentifier, out string skipReason)
    {
        sensorIdentifier = string.Empty;
        skipReason = string.Empty;

        if (!topic.StartsWith(SensorTopicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            skipReason = "nie_prefiks_zigbee2mqtt";
            return false;
        }

        if (topic.StartsWith(BridgeTopicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            skipReason = "most_bridge";
            return false;
        }

        // Tylko zigbee2mqtt/<jedna_nazwa> — pełny JSON stanu. Pomija .../availability, .../set, tryb „attribute”.
        var remainder = topic.AsSpan(SensorTopicPrefix.Length).Trim();
        if (remainder.IsEmpty)
        {
            skipReason = "pusty_identyfikator";
            return false;
        }

        if (remainder.Contains('/'))
        {
            skipReason = "podtemat_lub_tryb_attribute";
            return false;
        }

        sensorIdentifier = remainder.ToString();
        if (string.IsNullOrWhiteSpace(sensorIdentifier))
        {
            skipReason = "pusty_identyfikator";
            return false;
        }

        skipReason = "";
        return true;
    }
}
