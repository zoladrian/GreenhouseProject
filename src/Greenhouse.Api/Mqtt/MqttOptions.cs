using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Api.Mqtt;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    /// <summary>
    /// Wyłącz dla testów integracyjnych API lub lokalnego dev bez brokera.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Jeśli false, API nie uruchamia ingestu (np. gdy aktywny jest Worker).
    /// </summary>
    public bool EnableInApiHost { get; init; } = true;

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1883;

    [Required]
    public string TopicFilter { get; init; } = "zigbee2mqtt/#";
}
