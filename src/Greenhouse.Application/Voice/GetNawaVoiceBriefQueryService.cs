using System.Globalization;
using System.Text;
using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Nawy;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Voice;

public sealed class GetNawaVoiceBriefQueryService
{
    private static readonly TimeSpan HistoryLookback = TimeSpan.FromHours(72);

    private readonly VoiceOptions _voice;
    private readonly INawaRepository _nawy;
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetNawaVoiceBriefQueryService(
        IOptions<VoiceOptions> voiceOptions,
        INawaRepository nawy,
        ISensorRepository sensors,
        ISensorReadingRepository readings)
    {
        _voice = voiceOptions.Value;
        _nawy = nawy;
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<NawaVoiceBriefDto?> ExecuteAsync(Guid nawaId, CancellationToken cancellationToken)
    {
        var nawa = await _nawy.GetByIdAsync(nawaId, cancellationToken);
        if (nawa is null)
            return null;

        var tz = ResolveTimeZone(_voice.TimeZoneId);
        var pl = CultureInfo.GetCultureInfo("pl-PL");
        var utcNow = DateTime.UtcNow;

        var allSensors = await _sensors.ListAsync(cancellationToken);
        var nawaSensors = allSensors.Where(s => s.NawaId == nawa.Id).ToList();
        var sensorIds = nawaSensors.Select(s => s.Id).ToList();

        var sb = new StringBuilder();
        sb.Append("Nawa ").Append(nawa.Name).Append(". ");
        if (!nawa.IsActive)
            sb.Append("Uwaga: nawa jest oznaczona jako nieaktywna. ");

        if (sensorIds.Count == 0)
        {
            sb.Append("Brak przypisanych czujników — nie ma po czym oceniać wilgotności ani temperatury.");
            return new NawaVoiceBriefDto(nawa.Name, sb.ToString());
        }

        var latestReadings = await _readings.GetLatestPerSensorAsync(sensorIds, cancellationToken);
        var moistures = latestReadings.Where(r => r.SoilMoisture.HasValue).Select(r => r.SoilMoisture!.Value).ToList();
        var moistureReadingCount = moistures.Count;
        var temperatures = latestReadings.Where(r => r.Temperature.HasValue).Select(r => r.Temperature!.Value).ToList();
        var minMoisture = moistures.Count > 0 ? moistures.Min() : (decimal?)null;
        var maxMoisture = moistures.Count > 0 ? moistures.Max() : (decimal?)null;
        var moistureSpread = moistures.Count >= 2 && minMoisture.HasValue && maxMoisture.HasValue
            ? Math.Round(maxMoisture.Value - minMoisture.Value, 2)
            : (decimal?)null;
        var avgTemp = temperatures.Count > 0 ? Math.Round(temperatures.Average(), 2) : (decimal?)null;
        var oldestReading = latestReadings.Count > 0 ? latestReadings.Min(r => r.ReceivedAtUtc) : (DateTime?)null;

        var thresholds = nawa.GetMoistureThresholds();
        var status = OperatorStatusCalculator.Calculate(
            sensorIds.Count,
            moistureReadingCount,
            minMoisture,
            maxMoisture,
            oldestReading,
            thresholds,
            utcNow);

        sb.Append("Stan wilgotności: ").Append(StatusLabel(status)).Append(' ');

        var mMin = nawa.MoistureMin;
        var mMax = nawa.MoistureMax;
        var tMin = nawa.TemperatureMin;
        var tMax = nawa.TemperatureMax;

        if (mMin.HasValue || mMax.HasValue)
        {
            sb.Append("Progi wilgotności: ");
            if (mMin.HasValue)
                sb.Append("podlej przy ").Append(Fmt(mMin.Value)).Append(" procentach lub mniej");
            if (mMin.HasValue && mMax.HasValue)
                sb.Append(", ");
            if (mMax.HasValue)
                sb.Append("za mokro przy ").Append(Fmt(mMax.Value)).Append(" procentach lub więcej");
            sb.Append(". ");
        }
        else
            sb.Append("Nie ustawiono progów wilgotności dla tej nawy. ");

        if (minMoisture.HasValue && maxMoisture.HasValue)
            sb.Append("Teraz: najsuchszy punkt ").Append(Fmt(minMoisture.Value)).Append(" procent, najmokrzejszy ")
                .Append(Fmt(maxMoisture.Value)).Append(" procent. ");
        else if (moistureReadingCount == 0)
            sb.Append("Brak aktualnych odczytów wilgotności z czujników. ");

        if (avgTemp.HasValue)
            sb.Append("Średnia temperatura z czujników około ").Append(Fmt(avgTemp.Value)).Append(" stopni Celsjusza. ");
        else
            sb.Append("Brak aktualnych odczytów temperatury. ");

        if (tMin.HasValue || tMax.HasValue)
        {
            sb.Append("Progi temperatury: ");
            if (tMin.HasValue)
                sb.Append("minimum ").Append(Fmt(tMin.Value)).Append(" stopni");
            if (tMin.HasValue && tMax.HasValue)
                sb.Append(", ");
            if (tMax.HasValue)
                sb.Append("maksimum ").Append(Fmt(tMax.Value)).Append(" stopni");
            sb.Append(". ");
        }

        var historyFrom = utcNow - HistoryLookback;
        var history = await _readings.GetBySensorIdsAsync(sensorIds, historyFrom, utcNow, cancellationToken);
        var perSensor = VoiceNawaTimeline.BuildPerSensorLists(history, sensorIds);

        if (mMin.HasValue && (status == OperatorStatus.Dry || status == OperatorStatus.Conflict))
        {
            var since = VoiceNawaTimeline.EstimateMoistureDrySinceUtc(perSensor, sensorIds, mMin.Value, utcNow, HistoryLookback);
            if (since.HasValue)
                sb.Append("Suchość według najsuchszego czujnika szacuję co najmniej od ")
                    .Append(FormatLocal(since.Value, tz, pl))
                    .Append(", na podstawie historii z ostatnich 72 godzin. ");
            else
                sb.Append("Suchość jest widoczna teraz, ale nie udało się oszacować momentu startu z historii 72 godzin. ");
        }

        if (mMax.HasValue && (status == OperatorStatus.Warning || status == OperatorStatus.Conflict))
        {
            var sinceW = VoiceNawaTimeline.EstimateMoistureWetSinceUtc(perSensor, sensorIds, mMax.Value, utcNow, HistoryLookback);
            if (sinceW.HasValue)
                sb.Append("Stan za mokro według najmokrzejszego czujnika szacuję co najmniej od ")
                    .Append(FormatLocal(sinceW.Value, tz, pl))
                    .Append(". ");
            else
                sb.Append("Przemoczenie jest widoczne teraz, ale nie udało się oszacować momentu startu z historii. ");
        }

        if (status == OperatorStatus.UnevenMoisture && moistureSpread.HasValue)
            sb.Append("Duży rozstrzał między czujnikami: około ").Append(Fmt(moistureSpread.Value))
                .Append(" punktów procentowych — nie da się podać jednej godziny „od kiedy”, bo problem wynika z różnicy odczytów jednocześnie. ");

        if (status == OperatorStatus.NoData)
            sb.Append("Brak świeżych danych do oceny statusu — sprawdź baterie czujników i połączenie. ");

        if (avgTemp.HasValue && (tMin.HasValue || tMax.HasValue))
        {
            var low = tMin.HasValue && avgTemp < tMin;
            var high = tMax.HasValue && avgTemp > tMax;
            if (!low && !high)
                sb.Append("Temperatura poprawna względem progów. ");
            else
            {
                sb.Append(low
                    ? "Uwaga, anomalia temperatury: średnia jest poniżej progu. "
                    : "Uwaga, anomalia temperatury: średnia jest powyżej progu. ");
                var sinceT = VoiceNawaTimeline.EstimateTemperatureOutOfRangeSinceUtc(
                    perSensor, sensorIds, tMin, tMax, utcNow, HistoryLookback);
                if (sinceT.HasValue)
                    sb.Append("Szacuję utrzymywanie się tego stanu co najmniej od ")
                        .Append(FormatLocal(sinceT.Value, tz, pl))
                        .Append(". ");
                else
                    sb.Append("Nie udało się oszacować momentu startu z historii 72 godzin. ");
            }
        }
        else if (!avgTemp.HasValue && (tMin.HasValue || tMax.HasValue))
            sb.Append("Nie ma danych o temperaturze — nie oceniam względem progów. ");
        else if (avgTemp.HasValue && !tMin.HasValue && !tMax.HasValue)
            sb.Append("Temperatura: brak progów alertu w nawie — nie klasyfikuję jako anomalię. ");

        return new NawaVoiceBriefDto(nawa.Name, sb.ToString().Trim());
    }

    private static string StatusLabel(OperatorStatus s) => s switch
    {
        OperatorStatus.Ok => "w normie według progów i rozstrzału.",
        OperatorStatus.Warning => "za mokro — przekroczony próg górny wilgotności.",
        OperatorStatus.Dry => "sucho — najniższy odczyt poniżej progu podlania.",
        OperatorStatus.NoData => "brak danych lub dane są zbyt stare.",
        OperatorStatus.Conflict => "sprzeczne odczyty — jeden czujnik wskazuje sucho, inny za mokro.",
        OperatorStatus.UnevenMoisture => "duży rozstrzał między czujnikami przy braku alarmu sucho lub mokro.",
        _ => "nieznany.",
    };

    private static string Fmt(decimal d) => d.ToString("0.#", CultureInfo.GetCultureInfo("pl-PL"));

    private static string FormatLocal(DateTime utc, TimeZoneInfo tz, CultureInfo culture) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz).ToString("g", culture);

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
