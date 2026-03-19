namespace Greenhouse.Workers;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 1883;

    public string TopicFilter { get; init; } = "zigbee2mqtt/#";
}
