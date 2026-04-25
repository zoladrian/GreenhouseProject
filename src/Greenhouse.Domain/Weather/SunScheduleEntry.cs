namespace Greenhouse.Domain.Weather;

public sealed class SunScheduleEntry
{
    private SunScheduleEntry()
    {
    }

    public DateOnly Date { get; private set; }

    /// <summary>HH:mm lokalnie.</summary>
    public string SunriseLocal { get; private set; } = string.Empty;

    /// <summary>HH:mm lokalnie.</summary>
    public string SunsetLocal { get; private set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; private set; }

    public static SunScheduleEntry Create(DateOnly date, string sunriseLocal, string sunsetLocal)
    {
        ValidateTime(sunriseLocal, nameof(sunriseLocal));
        ValidateTime(sunsetLocal, nameof(sunsetLocal));
        return new SunScheduleEntry
        {
            Date = date,
            SunriseLocal = sunriseLocal.Trim(),
            SunsetLocal = sunsetLocal.Trim(),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateTimes(string sunriseLocal, string sunsetLocal)
    {
        ValidateTime(sunriseLocal, nameof(sunriseLocal));
        ValidateTime(sunsetLocal, nameof(sunsetLocal));
        SunriseLocal = sunriseLocal.Trim();
        SunsetLocal = sunsetLocal.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateTime(string value, string paramName)
    {
        if (!TimeOnly.TryParse(value, out _))
            throw new ArgumentException("Godzina musi mieć format HH:mm.", paramName);
    }
}
