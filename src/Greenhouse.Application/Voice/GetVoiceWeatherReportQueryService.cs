using System.Globalization;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Time;
using Greenhouse.Application.Weather;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Voice;

public sealed class GetVoiceWeatherReportQueryService
{
    private readonly VoiceOptions _voice;
    private readonly IClock _clock;
    private readonly IGreenhouseTimeZoneResolver _tz;
    private readonly WeatherControlConfigService _weatherControl;

    public GetVoiceWeatherReportQueryService(
        IOptions<VoiceOptions> voiceOptions,
        IClock clock,
        IGreenhouseTimeZoneResolver tz,
        WeatherControlConfigService weatherControl)
    {
        _voice = voiceOptions.Value;
        _clock = clock;
        _tz = tz;
        _weatherControl = weatherControl;
    }

    public async Task<VoiceWeatherReportDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var tz = _tz.Resolve(_voice.TimeZoneId);
        var nowUtc = _clock.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var culture = new CultureInfo("pl-PL");
        var timeStr = nowLocal.ToString("HH:mm", culture);
        var dateLong = nowLocal.ToString("D", culture);

        var s = await _weatherControl.GetCurrentStatusAsync(cancellationToken);
        var leadin = string.IsNullOrWhiteSpace(_voice.GreetingLeadin) ? "Dzień dobry" : _voice.GreetingLeadin.Trim();
        return new VoiceWeatherReportDto(
            leadin,
            timeStr,
            dateLong,
            s.RainStatus,
            s.LightStatus,
            s.IsNightBySchedule,
            s.RainIntensityRaw,
            s.IlluminanceRaw,
            s.SourceUtcTime);
    }
}
