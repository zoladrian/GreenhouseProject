namespace Greenhouse.Domain.Sensors;

public sealed class Sensor
{
    private Sensor()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Identyfikator z topicu MQTT Zigbee2MQTT (friendly_name lub IEEE).
    /// </summary>
    public string ExternalId { get; private set; } = string.Empty;

    public string? DisplayName { get; private set; }

    public SensorKind Kind { get; private set; }

    public Guid? NawaId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Sensor Register(string externalId, SensorKind kind = SensorKind.Unknown)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("Identyfikator czujnika jest wymagany.", nameof(externalId));
        }

        var trimmed = externalId.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException("Identyfikator czujnika nie może przekraczać 120 znaków.", nameof(externalId));
        }

        return new Sensor
        {
            Id = Guid.NewGuid(),
            ExternalId = trimmed,
            DisplayName = null,
            Kind = kind,
            NawaId = null,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void SetDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = null;
            return;
        }

        var trimmed = displayName.Trim();
        DisplayName = trimmed.Length > 120 ? trimmed[..120] : trimmed;
    }

    /// <summary>
    /// Zmiana stabilnego klucza z topicu (friendly name) na adres IEEE po pierwszym odczycie z polem <c>ieee_address</c> w JSON.
    /// </summary>
    public void RekeyExternalId(string newExternalId)
    {
        if (string.IsNullOrWhiteSpace(newExternalId))
        {
            throw new ArgumentException("Identyfikator czujnika jest wymagany.", nameof(newExternalId));
        }

        var trimmed = newExternalId.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException("Identyfikator czujnika nie może przekraczać 120 znaków.", nameof(newExternalId));
        }

        ExternalId = trimmed;
    }

    public void AssignToNawa(Guid nawaId)
    {
        if (nawaId == Guid.Empty)
        {
            throw new ArgumentException("Identyfikator nawy jest nieprawidłowy.", nameof(nawaId));
        }

        NawaId = nawaId;
    }

    public void UnassignFromNawa()
    {
        NawaId = null;
    }

    public void UpdateKind(SensorKind kind)
    {
        if (kind == SensorKind.Unknown)
            return;
        Kind = kind;
    }
}
