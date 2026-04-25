using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Weather;

public sealed class WeatherControlConfigService
{
    private readonly IWeatherControlConfigRepository _repo;
    private readonly ISunScheduleRepository _sunSchedule;
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public WeatherControlConfigService(
        IWeatherControlConfigRepository repo,
        ISunScheduleRepository sunSchedule,
        ISensorRepository sensors,
        ISensorReadingRepository readings)
    {
        _repo = repo;
        _sunSchedule = sunSchedule;
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<WeatherControlConfigDto> GetConfigAsync(CancellationToken cancellationToken)
    {
        var c = await _repo.GetOrCreateAsync(cancellationToken);
        return ToDto(c);
    }

    public async Task<WeatherControlConfigDto> UpdateAsync(
        decimal rainDetectedMinRaw,
        decimal highHumidityMinRaw,
        decimal sunnyMinRaw,
        decimal cloudyMaxRaw,
        string sunriseLocal,
        string sunsetLocal,
        string manualRainStatus,
        string manualLightStatus,
        CancellationToken cancellationToken)
    {
        var c = await _repo.GetOrCreateAsync(cancellationToken);
        c.Update(rainDetectedMinRaw, highHumidityMinRaw, sunnyMinRaw, cloudyMaxRaw, sunriseLocal, sunsetLocal, manualRainStatus, manualLightStatus);
        await _repo.SaveChangesAsync(cancellationToken);
        return ToDto(c);
    }

    public async Task<WeatherCurrentStatusDto> GetCurrentStatusAsync(CancellationToken cancellationToken)
    {
        var cfg = await _repo.GetOrCreateAsync(cancellationToken);
        var weatherSensorIds = (await _sensors.ListAsync(cancellationToken))
            .Where(s => s.Kind == Domain.Sensors.SensorKind.Weather)
            .Select(s => s.Id)
            .ToList();

        if (weatherSensorIds.Count == 0)
            return new WeatherCurrentStatusDto("unknown", "unknown", IsNightBySchedule(DateTime.UtcNow, cfg.SunriseLocal, cfg.SunsetLocal), null, null, null);

        var latest = await _readings.GetLatestPerSensorAsync(weatherSensorIds, cancellationToken);
        var row = latest.OrderByDescending(x => x.ReceivedAtUtc).FirstOrDefault();
        if (row is null)
            return new WeatherCurrentStatusDto("unknown", "unknown", IsNightBySchedule(DateTime.UtcNow, cfg.SunriseLocal, cfg.SunsetLocal), null, null, null);

        var forDay = await _sunSchedule.GetByDateAsync(DateOnly.FromDateTime(row.ReceivedAtUtc.ToLocalTime()), cancellationToken);
        var sunrise = forDay?.SunriseLocal ?? cfg.SunriseLocal;
        var sunset = forDay?.SunsetLocal ?? cfg.SunsetLocal;
        var isNight = IsNightBySchedule(row.ReceivedAtUtc, sunrise, sunset);
        var rainStatus = ResolveRainStatus(cfg.ManualRainStatus, row.Rain, row.RainIntensityRaw, cfg.RainDetectedMinRaw, cfg.HighHumidityMinRaw);
        var lightStatus = ResolveLightStatus(cfg.ManualLightStatus, row.IlluminanceRaw ?? row.IlluminanceAverage20MinRaw, cfg.SunnyMinRaw, cfg.CloudyMaxRaw, isNight);
        return new WeatherCurrentStatusDto(rainStatus, lightStatus, isNight, row.RainIntensityRaw, row.IlluminanceRaw ?? row.IlluminanceAverage20MinRaw, row.ReceivedAtUtc);
    }

    public static bool IsNightBySchedule(DateTime utcTime, string sunriseLocal, string sunsetLocal)
    {
        if (!TimeOnly.TryParse(sunriseLocal, out var sunrise)) sunrise = new TimeOnly(6, 0);
        if (!TimeOnly.TryParse(sunsetLocal, out var sunset)) sunset = new TimeOnly(20, 0);
        var t = TimeOnly.FromDateTime(utcTime.ToLocalTime());
        if (sunset > sunrise)
            return t < sunrise || t >= sunset;
        return t >= sunset && t < sunrise;
    }

    public async Task<IReadOnlyDictionary<DateOnly, (string Sunrise, string Sunset)>> GetScheduleMapAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var cfg = await _repo.GetOrCreateAsync(cancellationToken);
        var list = await _sunSchedule.GetRangeAsync(from, to, cancellationToken);
        var dict = list.ToDictionary(x => x.Date, x => (x.SunriseLocal, x.SunsetLocal));
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (!dict.ContainsKey(d))
                dict[d] = (cfg.SunriseLocal, cfg.SunsetLocal);
        }

        return dict;
    }

    public async Task<IReadOnlyList<SunScheduleEntryDto>> GetScheduleAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var list = await _sunSchedule.GetRangeAsync(from, to, cancellationToken);
        return list.Select(x => new SunScheduleEntryDto(x.Date, x.SunriseLocal, x.SunsetLocal)).ToList();
    }

    public async Task<SunScheduleImportResultDto> ImportCsvAsync(string csvContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            throw new ArgumentException("Plik CSV jest pusty.");

        var lines = csvContent
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();
        if (lines.Count == 0)
            throw new ArgumentException("Brak danych CSV.");

        var imported = new List<Domain.Weather.SunScheduleEntry>();
        var ignored = 0;
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                ignored++;
                continue;
            }

            if (!DateOnly.TryParse(parts[0].Trim(), out var date) ||
                !TimeOnly.TryParse(parts[1].Trim(), out var sunrise) ||
                !TimeOnly.TryParse(parts[2].Trim(), out var sunset))
            {
                ignored++;
                continue;
            }

            imported.Add(Domain.Weather.SunScheduleEntry.Create(date, sunrise.ToString("HH:mm"), sunset.ToString("HH:mm")));
        }

        await _sunSchedule.UpsertManyAsync(imported, cancellationToken);
        return new SunScheduleImportResultDto(imported.Count, ignored);
    }

    public static string ResolveRainStatus(string manual, bool? rain, decimal? intensity, decimal rainDetectedMin, decimal highHumidityMin)
    {
        if (!string.Equals(manual, "auto", StringComparison.OrdinalIgnoreCase))
            return manual;
        var i = intensity ?? 0m;
        if (i >= rainDetectedMin) return "raining";
        return "no-rain";
    }

    public static string ResolveLightStatus(string manual, decimal? illuminance, decimal sunnyMin, decimal cloudyMax, bool isNightBySchedule)
    {
        if (!string.Equals(manual, "auto", StringComparison.OrdinalIgnoreCase))
            return manual;
        if (isNightBySchedule) return "night";
        var v = illuminance ?? 0m;
        if (v >= sunnyMin) return "sunny";
        if (v <= cloudyMax) return "cloudy";
        return "cloudy";
    }

    private static WeatherControlConfigDto ToDto(Domain.Weather.WeatherControlConfig c)
        => new(
            c.RainDetectedMinRaw,
            c.HighHumidityMinRaw,
            c.SunnyMinRaw,
            c.CloudyMaxRaw,
            c.SunriseLocal,
            c.SunsetLocal,
            c.ManualRainStatus,
            c.ManualLightStatus,
            c.UpdatedAtUtc);
}
