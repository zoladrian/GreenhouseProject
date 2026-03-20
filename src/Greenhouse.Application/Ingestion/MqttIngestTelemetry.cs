using System.Threading;
using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Ingestion;

public sealed class MqttIngestTelemetry : IMqttIngestTelemetry
{
    private long _brokerMessages;
    private long _skippedTopic;
    private long _readingsPersisted;

    public void NotifyBrokerMessageReceived() => Interlocked.Increment(ref _brokerMessages);

    public void NotifyIngestSkippedNonSensorTopic() => Interlocked.Increment(ref _skippedTopic);

    public void NotifyReadingPersisted() => Interlocked.Increment(ref _readingsPersisted);

    public MqttIngestTelemetrySnapshot GetSnapshot() =>
        new(
            Interlocked.Read(ref _brokerMessages),
            Interlocked.Read(ref _skippedTopic),
            Interlocked.Read(ref _readingsPersisted));
}
