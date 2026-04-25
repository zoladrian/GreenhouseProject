using System.Security.Cryptography;
using System.Text;

namespace Greenhouse.Domain.SensorReadings;

public sealed class SensorReading
{
    private SensorReading()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Stabilny identyfikator serii: zwykle taki sam jak <c>Sensor.ExternalId</c> (IEEE z Zigbee), nie nazwa z topicu MQTT.
    /// </summary>
    public string SensorIdentifier { get; private set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; private set; }

    public string Topic { get; private set; } = string.Empty;

    public string RawPayloadJson { get; private set; } = string.Empty;

    /// <summary>
    /// Deterministyczny hash <c>topic + '\n' + RawPayloadJson</c> (lowercase hex SHA-256).
    /// Używany przez ingest do szybkiej, semantycznej dedup-policy odpornej na restart
    /// (Z2M retained, replay brokera) — porównanie po hashu w oknie czasowym ±2 s
    /// jest tańsze i bezpieczniejsze niż equality całego JSON-a.
    /// </summary>
    public string? PayloadHash { get; private set; }

    public decimal? SoilMoisture { get; private set; }

    public decimal? Temperature { get; private set; }

    public int? Battery { get; private set; }

    public int? LinkQuality { get; private set; }

    public bool? Rain { get; private set; }

    public decimal? RainIntensityRaw { get; private set; }

    public decimal? IlluminanceRaw { get; private set; }

    public decimal? IlluminanceAverage20MinRaw { get; private set; }

    public decimal? IlluminanceMaximumTodayRaw { get; private set; }

    public bool? CleaningReminder { get; private set; }

    /// <summary>
    /// Opcjonalne powiązanie z zarejestrowanym czujnikiem w systemie (klucz obcy).
    /// </summary>
    public Guid? SensorId { get; private set; }

    public static SensorReading Create(
        string sensorIdentifier,
        DateTime receivedAtUtc,
        string topic,
        string rawPayloadJson,
        decimal? soilMoisture,
        decimal? temperature,
        int? battery,
        int? linkQuality,
        Guid? sensorId)
    {
        return Create(
            sensorIdentifier,
            receivedAtUtc,
            topic,
            rawPayloadJson,
            soilMoisture,
            temperature,
            battery,
            linkQuality,
            rain: null,
            rainIntensityRaw: null,
            illuminanceRaw: null,
            illuminanceAverage20MinRaw: null,
            illuminanceMaximumTodayRaw: null,
            cleaningReminder: null,
            sensorId: sensorId);
    }

    public static SensorReading Create(
        string sensorIdentifier,
        DateTime receivedAtUtc,
        string topic,
        string rawPayloadJson,
        decimal? soilMoisture,
        decimal? temperature,
        int? battery,
        int? linkQuality,
        bool? rain = null,
        decimal? rainIntensityRaw = null,
        decimal? illuminanceRaw = null,
        decimal? illuminanceAverage20MinRaw = null,
        decimal? illuminanceMaximumTodayRaw = null,
        bool? cleaningReminder = null,
        Guid? sensorId = null)
    {
        if (string.IsNullOrWhiteSpace(sensorIdentifier))
        {
            throw new ArgumentException("Sensor identifier is required.", nameof(sensorIdentifier));
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        if (string.IsNullOrWhiteSpace(rawPayloadJson))
        {
            throw new ArgumentException("Raw payload is required.", nameof(rawPayloadJson));
        }

        return new SensorReading
        {
            Id = Guid.NewGuid(),
            SensorIdentifier = sensorIdentifier,
            ReceivedAtUtc = DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc),
            Topic = topic,
            RawPayloadJson = rawPayloadJson,
            PayloadHash = ComputePayloadHash(topic, rawPayloadJson),
            SoilMoisture = soilMoisture,
            Temperature = temperature,
            Battery = battery,
            LinkQuality = linkQuality,
            Rain = rain,
            RainIntensityRaw = rainIntensityRaw,
            IlluminanceRaw = illuminanceRaw,
            IlluminanceAverage20MinRaw = illuminanceAverage20MinRaw,
            IlluminanceMaximumTodayRaw = illuminanceMaximumTodayRaw,
            CleaningReminder = cleaningReminder,
            SensorId = sensorId
        };
    }

    /// <summary>
    /// Publiczny helper, żeby ingestion mógł policzyć hash zanim trafi do <see cref="Create"/>
    /// i sprawdzić duplikat w repozytorium bez konstruowania pełnej encji.
    /// </summary>
    public static string ComputePayloadHash(string topic, string rawPayloadJson)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(rawPayloadJson);
        Span<byte> buffer = stackalloc byte[32];
        var bytes = Encoding.UTF8.GetBytes(string.Concat(topic, "\n", rawPayloadJson));
        if (!SHA256.TryHashData(bytes, buffer, out _))
            throw new InvalidOperationException("Nie udało się policzyć SHA-256 z payloadu.");
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }
}
