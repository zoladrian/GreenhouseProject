using Greenhouse.Application.Abstractions;
using MQTTnet;
using MQTTnet.Protocol;

namespace Greenhouse.Workers;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new MqttOptions();
        _configuration.GetSection(MqttOptions.SectionName).Bind(options);
        if (!options.Enabled || !options.EnableInWorkerHost)
        {
            _logger.LogInformation("MQTT worker ingestion is disabled by configuration.");
            return;
        }

        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += async eventArgs =>
        {
            var topic = eventArgs.ApplicationMessage.Topic;
            var payload = eventArgs.ApplicationMessage.ConvertPayloadToString();

            using var scope = _scopeFactory.CreateScope();
            var ingestion = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
            var message = new IncomingMqttMessage(topic, payload, DateTime.UtcNow);
            await ingestion.IngestAsync(message, stoppingToken);
        };

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(options.Host, options.Port)
            .Build();

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
                    _logger.LogInformation("MQTT connected to {Host}:{Port}", options.Host, options.Port);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "MQTT connection loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
