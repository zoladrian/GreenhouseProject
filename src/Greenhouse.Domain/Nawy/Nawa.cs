namespace Greenhouse.Domain.Nawy;

public sealed class Nawa
{
    private Nawa()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? PlantNote { get; private set; }

    public bool IsActive { get; private set; }

    public decimal? MoistureMin { get; private set; }

    public decimal? MoistureMax { get; private set; }

    public decimal? TemperatureMin { get; private set; }

    public decimal? TemperatureMax { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public MoistureThresholds GetMoistureThresholds() => new(MoistureMin, MoistureMax);

    public TemperatureThresholds GetTemperatureThresholds() => new(TemperatureMin, TemperatureMax);

    public static Nawa Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nazwa nawy jest wymagana.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException("Nazwa nawy nie może przekraczać 120 znaków.", nameof(name));
        }

        return new Nawa
        {
            Id = Guid.NewGuid(),
            Name = trimmed,
            Description = NormalizeDescription(description),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nazwa nawy jest wymagana.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException("Nazwa nawy nie może przekraczać 120 znaków.", nameof(name));
        }

        Name = trimmed;
    }

    public void UpdateDescription(string? description)
    {
        Description = NormalizeDescription(description);
    }

    public void SetPlantNote(string? plantNote)
    {
        PlantNote = NormalizePlantNote(plantNote);
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void UpdateMoistureThresholds(decimal? min, decimal? max)
    {
        if (min.HasValue && max.HasValue && min.Value >= max.Value)
        {
            throw new ArgumentException("Próg minimalny wilgotności musi być mniejszy od maksymalnego.");
        }

        MoistureMin = min;
        MoistureMax = max;
    }

    public void UpdateTemperatureThresholds(decimal? min, decimal? max)
    {
        if (min.HasValue && max.HasValue && min.Value >= max.Value)
        {
            throw new ArgumentException("Próg minimalny temperatury musi być mniejszy od maksymalnego.");
        }

        TemperatureMin = min;
        TemperatureMax = max;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        return trimmed.Length > 500 ? trimmed[..500] : trimmed;
    }

    private static string? NormalizePlantNote(string? plantNote)
    {
        if (string.IsNullOrWhiteSpace(plantNote))
        {
            return null;
        }

        var trimmed = plantNote.Trim();
        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }
}
