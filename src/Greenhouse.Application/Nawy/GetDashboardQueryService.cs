using System.Globalization;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Voice;
using Greenhouse.Domain.Nawy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Nawy;

public sealed class GetDashboardQueryService
{
    private static readonly TimeSpan PostWateringWetMax = TimeSpan.FromMinutes(30);

    private readonly INawaRepository _nawy;
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;
    private readonly ILogger<GetDashboardQueryService> _logger;
    private readonly IOptions<VoiceOptions> _voice;
    private readonly GetWateringEventsQueryService _watering;

    public GetDashboardQueryService(
        INawaRepository nawy,
        ISensorRepository sensors,
        ISensorReadingRepository readings,
        ILogger<GetDashboardQueryService> logger,
        IOptions<VoiceOptions> voiceOptions,
        GetWateringEventsQueryService watering)
    {
        _nawy = nawy;
        _sensors = sensors;
        _readings = readings;
        _logger = logger;
        _voice = voiceOptions;
        _watering = watering;
    }

    public async Task<IReadOnlyList<NawaSnapshotDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var nawy = await _nawy.ListAsync(cancellationToken);
        var allSensors = await _sensors.ListAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;

        var snapshots = new List<NawaSnapshotDto>();

        foreach (var nawa in nawy.Where(n => n.IsActive))
        {
            var nawaSensors = allSensors.Where(s => s.NawaId == nawa.Id).ToList();
            var sensorIds = nawaSensors.Select(s => s.Id).ToList();

            if (sensorIds.Count == 0)
            {
                snapshots.Add(BuildEmptySnapshot(nawa, utcNow));
                continue;
            }

            var latestReadings = await _readings.GetLatestPerSensorAsync(sensorIds, cancellationToken);

            var moistures = latestReadings
                .Where(r => r.SoilMoisture.HasValue)
                .Select(r => r.SoilMoisture!.Value)
                .ToList();

            var moistureReadingCount = moistures.Count;
            var temperatures = latestReadings
                .Where(r => r.Temperature.HasValue)
                .Select(r => r.Temperature!.Value)
                .ToList();

            var batteries = latestReadings
                .Where(r => r.Battery.HasValue)
                .Select(r => r.Battery!.Value)
                .ToList();

            var avgMoisture = moistures.Count > 0 ? Math.Round(moistures.Average(), 2) : (decimal?)null;
            var minMoisture = moistures.Count > 0 ? moistures.Min() : (decimal?)null;
            var maxMoisture = moistures.Count > 0 ? moistures.Max() : (decimal?)null;
            var moistureSpread = moistures.Count >= 2 && minMoisture.HasValue && maxMoisture.HasValue
                ? Math.Round(maxMoisture.Value - minMoisture.Value, 2)
                : (decimal?)null;
            var avgTemp = temperatures.Count > 0 ? Math.Round(temperatures.Average(), 2) : (decimal?)null;
            var lowestBattery = batteries.Count > 0 ? batteries.Min() : (int?)null;
            var oldestReading = latestReadings.Count > 0
                ? latestReadings.Min(r => r.ReceivedAtUtc)
                : (DateTime?)null;

            var thresholds = nawa.GetMoistureThresholds();
            var status = OperatorStatusCalculator.Calculate(
                sensorIds.Count,
                moistureReadingCount,
                minMoisture,
                maxMoisture,
                oldestReading,
                thresholds,
                utcNow);

            if (status == OperatorStatus.Warning && thresholds.Max.HasValue)
            {
                var sensorIdsWithMoisture = latestReadings
                    .Where(r => r.SoilMoisture.HasValue && r.SensorId.HasValue)
                    .Select(r => r.SensorId!.Value)
                    .Distinct()
                    .ToList();

                if (sensorIdsWithMoisture.Count > 0)
                {
                    var historyFrom = utcNow.AddHours(-48);
                    var history = await _readings.GetBySensorIdsAsync(sensorIdsWithMoisture, historyFrom, utcNow, cancellationToken);
                    var perSensor = VoiceNawaTimeline.BuildPerSensorLists(history, sensorIdsWithMoisture);
                    var wetMinutes = VoiceNawaTimeline.EstimateContinuousTooWetMinutesFromNow(
                        perSensor,
                        sensorIdsWithMoisture,
                        thresholds.Max.Value,
                        utcNow,
                        maxLookbackMinutes: 240);

                    if (wetMinutes > 0 && TimeSpan.FromMinutes(wetMinutes) < PostWateringWetMax)
                        status = OperatorStatus.PostWatering;
                }
            }

            LogNawaSnapshot(nawa.Name, nawa.Id, status, sensorIds.Count, moistureReadingCount, minMoisture, maxMoisture, moistureSpread, thresholds);

            string? wateringNote = null;
            if (status is OperatorStatus.Dry or OperatorStatus.Conflict or OperatorStatus.PostWatering)
            {
                var last = await _watering.TryGetLastWateringEventAsync(
                    nawa.Id,
                    utcNow - VoiceWateringSpeech.DefaultWateringLookback,
                    utcNow,
                    cancellationToken);
                var tz = ResolveTimeZone(_voice.Value.TimeZoneId);
                var pl = CultureInfo.GetCultureInfo("pl-PL");
                var note = status == OperatorStatus.PostWatering
                    ? VoiceWateringSpeech.ForPostWateringContext(last, utcNow, tz, pl)
                    : VoiceWateringSpeech.ForDrySinceWatering(last, utcNow, tz, pl);
                wateringNote = note.Trim();
                if (wateringNote.Length == 0)
                    wateringNote = null;
            }

            snapshots.Add(new NawaSnapshotDto(
                nawa.Id, nawa.Name, nawa.PlantNote,
                status, sensorIds.Count, moistureReadingCount,
                avgMoisture, minMoisture, maxMoisture, moistureSpread, avgTemp,
                lowestBattery, oldestReading, utcNow,
                nawa.MoistureMin, nawa.MoistureMax, nawa.TemperatureMin, nawa.TemperatureMax,
                wateringNote));
        }

        return snapshots;
    }

    private void LogNawaSnapshot(
        string nawaName,
        Guid nawaId,
        OperatorStatus status,
        int assignedSensors,
        int moistureReadings,
        decimal? minM,
        decimal? maxM,
        decimal? spread,
        MoistureThresholds th)
    {
        _logger.LogDebug(
            "Dashboard nawa={NawaName} ({NawaId}): status={Status}, czujniki={Assigned}, zWilgotnoscia={MoistureN}, min={Min}, max={Max}, rozstrzal={Spread}, progi min={ThMin} max={ThMax}",
            nawaName, nawaId, status, assignedSensors, moistureReadings, minM, maxM, spread, th.Min, th.Max);

        if (status == OperatorStatus.Conflict)
        {
            _logger.LogWarning(
                "Nawa {NawaName} ({NawaId}): sprzeczne odczyty wilgotności (min={Min} max={Max}) względem progów min={ThMin} max={ThMax}",
                nawaName, nawaId, minM, maxM, th.Min, th.Max);
        }
        else if (status == OperatorStatus.UnevenMoisture)
        {
            _logger.LogInformation(
                "Nawa {NawaName} ({NawaId}): duży rozstrzał czujników rozstrzal={Spread}% (min={Min} max={Max})",
                nawaName, nawaId, spread, minM, maxM);
        }
        else if (status == OperatorStatus.Dry)
        {
            _logger.LogInformation(
                "Nawa {NawaName} ({NawaId}): sucho (najniższy odczyt min={Min}, próg podlania={ThMin})",
                nawaName, nawaId, minM, th.Min);
        }
        else if (status == OperatorStatus.Warning)
        {
            _logger.LogInformation(
                "Nawa {NawaName} ({NawaId}): za mokro (najwyższy odczyt max={Max}, próg={ThMax})",
                nawaName, nawaId, maxM, th.Max);
        }
        else if (status == OperatorStatus.PostWatering)
        {
            _logger.LogInformation(
                "Nawa {NawaName} ({NawaId}): po podlaniu / krotkie przemoczenie (max={Max}, prog={ThMax}, <30 min wg historii)",
                nawaName, nawaId, maxM, th.Max);
        }
    }

    private static NawaSnapshotDto BuildEmptySnapshot(Nawa nawa, DateTime utcNow) =>
        new(nawa.Id, nawa.Name, nawa.PlantNote,
            OperatorStatus.NoData, 0, 0,
            null, null, null, null, null,
            null, null, utcNow,
            nawa.MoistureMin, nawa.MoistureMax, nawa.TemperatureMin, nawa.TemperatureMax,
            null);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

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
