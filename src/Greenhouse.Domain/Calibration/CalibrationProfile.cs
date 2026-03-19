namespace Greenhouse.Domain.Calibration;

public sealed class CalibrationProfile
{
    private CalibrationProfile() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal DryRawValue { get; private set; }
    public decimal WetRawValue { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static CalibrationProfile Create(string name, decimal dryRawValue, decimal wetRawValue)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nazwa profilu kalibracji jest wymagana.", nameof(name));

        if (dryRawValue >= wetRawValue)
            throw new ArgumentException("Wartość sucha musi być mniejsza od mokrej.", nameof(dryRawValue));

        return new CalibrationProfile
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            DryRawValue = dryRawValue,
            WetRawValue = wetRawValue,
            IsDefault = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Przelicza surową wartość na procent wilgotności (0–100%).
    /// </summary>
    public decimal CalibrateToPercent(decimal rawValue)
    {
        var range = WetRawValue - DryRawValue;
        if (range == 0) return 0;
        var calibrated = (rawValue - DryRawValue) / range * 100m;
        return Math.Clamp(Math.Round(calibrated, 2), 0m, 100m);
    }

    public void SetAsDefault(bool isDefault) => IsDefault = isDefault;

    public void Update(string name, decimal dryRawValue, decimal wetRawValue)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nazwa profilu kalibracji jest wymagana.", nameof(name));
        if (dryRawValue >= wetRawValue)
            throw new ArgumentException("Wartość sucha musi być mniejsza od mokrej.", nameof(dryRawValue));

        Name = name.Trim();
        DryRawValue = dryRawValue;
        WetRawValue = wetRawValue;
    }
}
