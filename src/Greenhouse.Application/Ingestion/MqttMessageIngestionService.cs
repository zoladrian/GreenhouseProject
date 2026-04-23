using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Domain.Sensors;
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
                        parsed.LinkQuality.HasValue || parsed.Rain.HasValue || parsed.RainIntensityRaw.HasValue ||
                        parsed.IlluminanceRaw.HasValue || parsed.IlluminanceAverage20MinRaw.HasValue ||
                        parsed.IlluminanceMaximumTodayRaw.HasValue || parsed.CleaningReminder.HasValue;
        var payloadTrim = message.Payload.TrimStart();
        if (!hasMetric && payloadTrim.StartsWith('{'))
        {
            _logger.LogDebug(
                "MQTT topic={Topic}, czujnik={Sensor}: JSON bez obsługiwanych pól metryk (tryb attribute w Z2M?). Fragment={Snippet}",
                message.Topic,
                externalId,
                Snippet(message.Payload, 160));
        }

        var ensured = await _sensorProvisioning.EnsureSensorAsync(
            new EnsureSensorInput(sensorIdentifier, externalId, ResolveSensorKind(parsed)),
            cancellationToken);
        if (ensured.CreatedNew)
        {
            _logger.LogInformation(
                "Zarejestrowano nowy czujnik z MQTT: topic={TopicName}, klucz={CanonicalId}, SensorId={SensorId}",
                sensorIdentifier,
                externalId,
                ensured.SensorId);
        }

        // Stabilny identyfikator wiersza = ExternalId czujnika (IEEE z JSON lub tymczasowo fragment topicu),
        // żeby zmiana friendly name w Z2M nie rozszczepiała historii na wiele serii.
        var reading = SensorReading.Create(
            externalId,
            message.ReceivedAtUtc,
            message.Topic,
            message.Payload,
            parsed.SoilMoisture,
            parsed.Temperature,
            parsed.Battery,
            parsed.LinkQuality,
            parsed.Rain,
            parsed.RainIntensityRaw,
            parsed.IlluminanceRaw,
            parsed.IlluminanceAverage20MinRaw,
            parsed.IlluminanceMaximumTodayRaw,
            parsed.CleaningReminder,
            ensured.SensorId);

        await _readingRepository.AddAsync(reading, cancellationToken);
        _telemetry.NotifyReadingPersisted();

        _logger.LogTrace(
            "MQTT zapis odczytu ExternalId={ExternalId}, wilgotność={M}, temp={T}, bateria={B}, LQ={Lq}, rain={Rain}, rain_intensity={RainIntensity}, illum_raw={IllRaw}",
            externalId,
            parsed.SoilMoisture,
            parsed.Temperature,
            parsed.Battery,
            parsed.LinkQuality,
            parsed.Rain,
            parsed.RainIntensityRaw,
            parsed.IlluminanceRaw);
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

    private static SensorKind ResolveSensorKind(ParsedSensorPayload parsed)
    {
        var hasWeather = parsed.Rain.HasValue || parsed.RainIntensityRaw.HasValue ||
                         parsed.IlluminanceRaw.HasValue || parsed.IlluminanceAverage20MinRaw.HasValue ||
                         parsed.IlluminanceMaximumTodayRaw.HasValue || parsed.CleaningReminder.HasValue;
        if (hasWeather)
            return SensorKind.Weather;

        var hasSoil = parsed.SoilMoisture.HasValue || parsed.Temperature.HasValue;
        return hasSoil ? SensorKind.Soil : SensorKind.Unknown;
    }
}
