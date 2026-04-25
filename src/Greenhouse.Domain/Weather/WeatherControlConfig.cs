namespace Greenhouse.Domain.Weather;

public sealed class WeatherControlConfig
{
    private WeatherControlConfig()
    {
    }

    public int Id { get; private set; }

    public decimal RainDetectedMinRaw { get; private set; }

    public decimal HighHumidityMinRaw { get; private set; }

    public decimal SunnyMinRaw { get; private set; }

    public decimal CloudyMaxRaw { get; private set; }

    /// <summary>HH:mm (lokalna strefa szklarni).</summary>
    public string SunriseLocal { get; private set; } = "06:00";

    /// <summary>HH:mm (lokalna strefa szklarni).</summary>
    public string SunsetLocal { get; private set; } = "20:00";

    /// <summary>auto / raining / no-rain / high-humidity.</summary>
    public string ManualRainStatus { get; private set; } = "auto";

    /// <summary>auto / sunny / cloudy / night.</summary>
    public string ManualLightStatus { get; private set; } = "auto";

    public DateTime UpdatedAtUtc { get; private set; }

    public static WeatherControlConfig CreateDefault()
    {
        return new WeatherControlConfig
        {
            Id = 1,
            RainDetectedMinRaw = 120m,
            HighHumidityMinRaw = 20m,
            SunnyMinRaw = 1600m,
            CloudyMaxRaw = 1000m,
            SunriseLocal = "06:00",
            SunsetLocal = "20:00",
            ManualRainStatus = "auto",
            ManualLightStatus = "auto",
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        decimal rainDetectedMinRaw,
        decimal highHumidityMinRaw,
        decimal sunnyMinRaw,
        decimal cloudyMaxRaw,
        string sunriseLocal,
        string sunsetLocal,
        string manualRainStatus,
        string manualLightStatus)
    {
        if (rainDetectedMinRaw < 0 || highHumidityMinRaw < 0 || sunnyMinRaw < 0 || cloudyMaxRaw < 0)
            throw new ArgumentException("Progi nie mogą być ujemne.");
        if (highHumidityMinRaw > rainDetectedMinRaw)
            throw new ArgumentException("Próg dużej wilgotności nie może być większy niż próg wykrycia opadu.");
        if (cloudyMaxRaw > sunnyMinRaw)
            throw new ArgumentException("Maks. próg zachmurzenia nie może być większy niż próg słonecznie.");

        if (!TimeOnly.TryParse(sunriseLocal, out _))
            throw new ArgumentException("Godzina wschodu ma nieprawidłowy format (HH:mm).");
        if (!TimeOnly.TryParse(sunsetLocal, out _))
            throw new ArgumentException("Godzina zachodu ma nieprawidłowy format (HH:mm).");

        ManualRainStatus = NormalizeManualRainStatus(manualRainStatus);
        ManualLightStatus = NormalizeManualLightStatus(manualLightStatus);
        RainDetectedMinRaw = rainDetectedMinRaw;
        HighHumidityMinRaw = highHumidityMinRaw;
        SunnyMinRaw = sunnyMinRaw;
        CloudyMaxRaw = cloudyMaxRaw;
        SunriseLocal = sunriseLocal;
        SunsetLocal = sunsetLocal;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeManualRainStatus(string value)
    {
        var v = value?.Trim().ToLowerInvariant() ?? "auto";
        return v switch
        {
            "auto" or "raining" or "no-rain" or "high-humidity" => v,
            _ => throw new ArgumentException("Nieprawidłowy status opadu.")
        };
    }

    private static string NormalizeManualLightStatus(string value)
    {
        var v = value?.Trim().ToLowerInvariant() ?? "auto";
        return v switch
        {
            "auto" or "sunny" or "cloudy" or "night" => v,
            _ => throw new ArgumentException("Nieprawidłowy status nasłonecznienia.")
        };
    }
}
