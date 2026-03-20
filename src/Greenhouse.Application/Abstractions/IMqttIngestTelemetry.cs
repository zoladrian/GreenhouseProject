namespace Greenhouse.Application.Abstractions;

/// <summary>Liczniki diagnostyczne ingestii MQTT (singleton).</summary>
public interface IMqttIngestTelemetry
{
    void NotifyBrokerMessageReceived();

    void NotifyIngestSkippedNonSensorTopic();

    void NotifyReadingPersisted();

    MqttIngestTelemetrySnapshot GetSnapshot();
}

public readonly record struct MqttIngestTelemetrySnapshot(
    long BrokerMessagesReceived,
    long TopicsSkippedNonSensor,
    long ReadingsPersisted);
