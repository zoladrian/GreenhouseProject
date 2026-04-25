using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Infrastructure.Hosting;

/// <summary>
/// Konfiguracja brokera MQTT — wspólna dla obu hostów (API/Workers). Wybór, w którym procesie ingest
/// faktycznie pracuje, jest sterowany flagami <see cref="EnableInApiHost"/> / <see cref="EnableInWorkerHost"/>.
/// W produkcji włączamy w jednym hoście — inaczej dwa procesy konkurują o ten sam <c>ClientId</c> u brokera.
/// </summary>
public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public bool Enabled { get; init; } = true;

    /// <summary>API uruchamia hosted service ingestu.</summary>
    public bool EnableInApiHost { get; init; } = true;

    /// <summary>Workers uruchamia hosted service ingestu (tylko gdy nie korzystamy z API).</summary>
    public bool EnableInWorkerHost { get; init; }

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1883;

    [Required]
    public string TopicFilter { get; init; } = "zigbee2mqtt/#";
}
