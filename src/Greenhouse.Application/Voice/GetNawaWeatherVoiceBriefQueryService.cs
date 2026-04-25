using System.Globalization;
using System.Text;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Time;
using Greenhouse.Application.Weather;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Voice;

public sealed class GetNawaWeatherVoiceBriefQueryService
{
    private readonly VoiceOptions _voice;
    private readonly INawaRepository _nawy;
    private readonly GetWeatherSeriesQueryService _weatherSeries;
    private readonly IClock _clock;
    private readonly IGreenhouseTimeZoneResolver _tz;

    public GetNawaWeatherVoiceBriefQueryService(
        IOptions<VoiceOptions> voiceOptions,
        INawaRepository nawy,
        GetWeatherSeriesQueryService weatherSeries,
        IClock clock,
        IGreenhouseTimeZoneResolver tz)
    {
        _voice = voiceOptions.Value;
        _nawy = nawy;
        _weatherSeries = weatherSeries;
        _clock = clock;
        _tz = tz;
    }

    public async Task<NawaVoiceBriefDto?> ExecuteAsync(Guid nawaId, CancellationToken cancellationToken)
    {
        var nawa = await _nawy.GetByIdAsync(nawaId, cancellationToken);
        if (nawa is null)
            return null;

        var utcNow = _clock.UtcNow;
        var fromUtc = utcNow.AddHours(-24);
        var points = await _weatherSeries.ExecuteAsync(nawaId, null, fromUtc, utcNow, cancellationToken);
        var latest = points.OrderByDescending(x => x.UtcTime).FirstOrDefault();

        var sb = new StringBuilder();
        sb.Append("Nawa ").Append(nawa.Name).Append(". ");
        if (latest is null)
        {
            sb.Append("Brak danych pogodowych z ostatnich 24 godzin dla tej nawy.");
            return new NawaVoiceBriefDto(nawa.Name, sb.ToString());
        }

        var pl = CultureInfo.GetCultureInfo("pl-PL");
        var tz = _tz.Resolve(_voice.TimeZoneId);
        sb.Append("Raport deszczu i nasłonecznienia. ");
        sb.Append("Aktualnie opad: ").Append(RainLabel(latest.CurrentRainStatus)).Append(". ");
        sb.Append("Aktualnie światło: ").Append(LightLabel(latest.CurrentLightStatus)).Append(". ");
        sb.Append(latest.IsNightBySchedule ? "Według harmonogramu jest noc. " : "Według harmonogramu jest dzień. ");

        if (latest.RainIntensityRaw.HasValue)
            sb.Append("Ostatnia intensywność opadu surowa: ").Append(latest.RainIntensityRaw.Value.ToString("0.#", pl)).Append(". ");
        if (latest.IlluminanceRaw.HasValue)
            sb.Append("Ostatnia jasność surowa: ").Append(latest.IlluminanceRaw.Value.ToString("0.#", pl)).Append(". ");

        var maxRain = points.Where(p => p.RainIntensityRaw.HasValue).Select(p => p.RainIntensityRaw!.Value).DefaultIfEmpty().Max();
        var maxLight = points.Where(p => p.IlluminanceRaw.HasValue).Select(p => p.IlluminanceRaw!.Value).DefaultIfEmpty().Max();
        if (maxRain > 0)
            sb.Append("Maksymalna intensywność opadu z 24 godzin: ").Append(maxRain.ToString("0.#", pl)).Append(". ");
        if (maxLight > 0)
            sb.Append("Maksymalna jasność z 24 godzin: ").Append(maxLight.ToString("0.#", pl)).Append(". ");

        var localLatest = TimeZoneInfo.ConvertTimeFromUtc(latest.UtcTime, tz).ToString("g", pl);
        sb.Append("Ostatni punkt pomiarowy: ").Append(localLatest).Append(".");

        return new NawaVoiceBriefDto(nawa.Name, sb.ToString().Trim());
    }

    private static string RainLabel(string value) => value switch
    {
        "raining" => "aktualnie pada",
        "no-rain" => "aktualnie nie pada",
        "high-humidity" => "aktualnie duża wilgotność",
        _ => "auto"
    };

    private static string LightLabel(string value) => value switch
    {
        "sunny" => "jest słonecznie",
        "cloudy" => "jest zachmurzenie",
        "night" => "jest noc",
        _ => "auto"
    };
}
