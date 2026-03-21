using System.Globalization;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Voice;

public sealed class GetVoiceDailyReportQueryService
{
    private readonly VoiceOptions _voice;
    private readonly INawaRepository _nawy;
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;
    private readonly GetWateringEventsQueryService _watering;

    public GetVoiceDailyReportQueryService(
        IOptions<VoiceOptions> voiceOptions,
        INawaRepository nawy,
        ISensorRepository sensors,
        ISensorReadingRepository readings,
        GetWateringEventsQueryService watering)
    {
        _voice = voiceOptions.Value;
        _nawy = nawy;
        _sensors = sensors;
        _readings = readings;
        _watering = watering;
    }

    public async Task<VoiceDailyReportDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var tz = ResolveTimeZone(_voice.TimeZoneId);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var dayStartLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, tz);

        var culture = new CultureInfo("pl-PL");
        var timeStr = nowLocal.ToString("HH:mm", culture);
        var dateLong = nowLocal.ToString("D", culture);

        var nawaList = await _nawy.ListAsync(cancellationToken);
        var activeOrdered = nawaList.Where(n => n.IsActive).OrderBy(n => n.CreatedAtUtc).ToList();
        var allSensors = await _sensors.ListAsync(cancellationToken);

        var lines = new List<NawaVoiceLineDto>();
        var order = 0;
        foreach (var nawa in activeOrdered)
        {
            order++;
            var sensorIds = allSensors.Where(s => s.NawaId == nawa.Id).Select(s => s.Id).ToList();
            if (sensorIds.Count == 0)
            {
                var (m0, t0) = VoiceAssessmentTexts.DailyAverages(
                    null, null,
                    nawa.MoistureMin, nawa.MoistureMax, nawa.TemperatureMin, nawa.TemperatureMax,
                    0, 0);
                lines.Add(new NawaVoiceLineDto(order, nawa.Name, null, null, 0, 0, m0, t0));
                continue;
            }

            var readings = await _readings.GetBySensorIdsAsync(sensorIds, dayStartUtc, nowUtc, cancellationToken);
            var moistures = readings.Where(r => r.SoilMoisture.HasValue).Select(r => r.SoilMoisture!.Value).ToList();
            var temps = readings.Where(r => r.Temperature.HasValue).Select(r => r.Temperature!.Value).ToList();

            var avgM = moistures.Count > 0 ? Math.Round(moistures.Average(), 1) : (decimal?)null;
            var avgT = temps.Count > 0 ? Math.Round(temps.Average(), 1) : (decimal?)null;

            var (m, t) = VoiceAssessmentTexts.DailyAverages(
                avgM, avgT,
                nawa.MoistureMin, nawa.MoistureMax, nawa.TemperatureMin, nawa.TemperatureMax,
                sensorIds.Count, readings.Count);

            if (readings.Count > 0 && nawa.MoistureMin.HasValue && avgM.HasValue && avgM.Value < nawa.MoistureMin.Value)
            {
                var last = await _watering.TryGetLastWateringEventAsync(
                    nawa.Id,
                    nowUtc - VoiceWateringSpeech.DefaultWateringLookback,
                    nowUtc,
                    cancellationToken);
                m = $"{m.TrimEnd()} {VoiceWateringSpeech.ForDrySinceWatering(last, nowUtc, tz, culture)}".Trim();
            }

            lines.Add(new NawaVoiceLineDto(order, nawa.Name, avgT, avgM, readings.Count, sensorIds.Count, m, t));
        }

        var leadin = string.IsNullOrWhiteSpace(_voice.GreetingLeadin) ? "Dzień dobry" : _voice.GreetingLeadin.Trim();
        return new VoiceDailyReportDto(leadin, timeStr, dateLong, lines);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
