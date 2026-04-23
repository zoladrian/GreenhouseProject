using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Workers;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public bool Enabled { get; init; } = true;

    public bool EnableInWorkerHost { get; init; } = false;

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1883;

    [Required]
    public string TopicFilter { get; init; } = "zigbee2mqtt/#";
}
