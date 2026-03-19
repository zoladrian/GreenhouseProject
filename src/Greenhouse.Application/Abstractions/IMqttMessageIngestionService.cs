namespace Greenhouse.Application.Abstractions;

public interface IMqttMessageIngestionService
{
    Task IngestAsync(IncomingMqttMessage message, CancellationToken cancellationToken);
}
