using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Greenhouse.Infrastructure.Hosting;

/// <summary>
/// Wspólna pętla ingestu MQTT: subskrypcja, retry połączenia, deleguje wiadomość do
/// <see cref="IMqttMessageIngestionService"/>. Uruchamiana w API albo Workers — nie w obu.
/// </summary>
public sealed class MqttIngestionHostedService : BackgroundService
{
    private readonly ILogger<MqttIngestionHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMqttIngestTelemetry _telemetry;
    private readonly IOptions<MqttOptions> _options;

    public MqttIngestionHostedService(
        ILogger<MqttIngestionHostedService> logger,
        IServiceScopeFactory scopeFactory,
        IMqttIngestTelemetry telemetry,
        IOptions<MqttOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _telemetry = telemetry;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("MQTT ingestion is disabled (Mqtt:Enabled=false).");
            return;
        }

        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        client.DisconnectedAsync += async disconnectArgs =>
        {
            if (disconnectArgs.Exception is not null)
            {
                _logger.LogWarning(
                    disconnectArgs.Exception,
                    "MQTT rozłączono (wyjątek). Powód={Reason}, typ wyjątku={ExType}",
                    disconnectArgs.Reason,
                    disconnectArgs.Exception.GetType().Name);
            }
            else
            {
                _logger.LogWarning("MQTT rozłączono. Powód={Reason}", disconnectArgs.Reason);
            }

            await Task.CompletedTask;
        };

        client.ApplicationMessageReceivedAsync += async eventArgs =>
        {
            _telemetry.NotifyBrokerMessageReceived();
            var topic = eventArgs.ApplicationMessage.Topic;
            var payload = eventArgs.ApplicationMessage.ConvertPayloadToString();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
                var message = new IncomingMqttMessage(topic, payload, DateTime.UtcNow);
                await ingestion.IngestAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "MQTT ingest — nieoczekiwany błąd (topic={Topic}, długość payload={Len})",
                    topic,
                    payload?.Length ?? 0);
            }
        };

        var mqttClientId = $"greenhouse-{Environment.ProcessId}";
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(options.Host, options.Port)
            .WithClientId(mqttClientId)
            .Build();

        var statsTask = RunMqttStatsLoopAsync(() => client.IsConnected, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(mqttOptions, stoppingToken);
                    await client.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic(options.TopicFilter)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build(), stoppingToken);
                    _logger.LogInformation(
                        "MQTT połączono: {Host}:{Port}, filtr={TopicFilter}, ClientId={ClientId}. Co ~2 min: podsumowanie liczników (broker / pominięcia / zapis).",
                        options.Host,
                        options.Port,
                        options.TopicFilter,
                        mqttClientId);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "MQTT — ponawianie połączenia za 5 s (Host={Host}, Port={Port})",
                    options.Host,
                    options.Port);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        try
        {
            await statsTask;
        }
        catch (OperationCanceledException)
        {
            /* shutdown */
        }
    }

    private async Task RunMqttStatsLoopAsync(Func<bool> isConnected, CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                var s = _telemetry.GetSnapshot();
                _logger.LogInformation(
                    "MQTT podsumowanie (od startu procesu): wiadomości z brokera={Broker}, pominięte tematy={Skip}, zapisane odczyty={Saved}, połączony={Connected}",
                    s.BrokerMessagesReceived,
                    s.TopicsSkippedNonSensor,
                    s.ReadingsPersisted,
                    isConnected());
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            /* normalne zatrzymanie */
        }
    }
}
