namespace Greenhouse.Api.Mqtt;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    /// <summary>
    /// Wyłącz dla testów integracyjnych API lub lokalnego dev bez brokera.
    /// </summary>
    public bool Enabled { get; init; } = true;

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 1883;

    public string TopicFilter { get; init; } = "zigbee2mqtt/#";
}
