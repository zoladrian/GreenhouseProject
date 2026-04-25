using System.Globalization;
using System.Text;
using Greenhouse.Application.Charts;

namespace Greenhouse.Application.Voice;

/// <summary>
/// Zdania głosowe o ostatnim wykrytym podlaniu (heurystyka skoku wilgotności).
/// Używa <see cref="PolishNumberWords"/> żeby liczby były wymawiane słownie (jedna minuta,
/// pięć minut, trzydzieści dni) — silniki TTS różnie czytają cyfry, słowa są jednoznaczne.
/// </summary>
internal static class VoiceWateringSpeech
{
    public static string ForPostWateringContext(
        WateringEventDto? last,
        DateTime utcNow,
        TimeZoneInfo tz,
        CultureInfo pl,
        TimeSpan wateringLookback)
    {
        if (last is null)
        {
            return $"W ostatnich {LookbackPhrase(wateringLookback)} nie znaleziono wyraźnego skoku wilgotności jak po podlaniu — dokładnej daty ostatniego podlania nie podam. ";
        }

        var sb = new StringBuilder();
        sb.Append("Ostatnie wyraźne zawilgocenie gleby, jak po podlaniu, szacuję na ")
            .Append(AgoPhrase(last.DetectedAtUtc, utcNow))
            .Append(". Lokalnie było to około ")
            .Append(FormatLocalDateTime(last.DetectedAtUtc, tz, pl))
            .Append(". ")
            .Append(KindClause(last.InferredKind));
        return sb.ToString();
    }

    public static string ForDrySinceWatering(
        WateringEventDto? last,
        DateTime utcNow,
        TimeZoneInfo tz,
        CultureInfo pl,
        TimeSpan wateringLookback)
    {
        if (last is null)
        {
            return $"W ostatnich {LookbackPhrase(wateringLookback)} nie wykryłem wyraźnego skoku wilgotności związanego z podlaniem — nie podam, ile minut minęło od podlania. ";
        }

        var mins = (int)Math.Floor((utcNow - last.DetectedAtUtc).TotalMinutes);
        if (mins < 0) mins = 0;

        var sb = new StringBuilder();
        sb.Append("Od ostatniego wykrytego podlania lub silnego zawilgocenia minęło ")
            .Append(PolishNumberWords.MinutesPhrase(mins))
            .Append(". To było około ")
            .Append(FormatLocalDateTime(last.DetectedAtUtc, tz, pl))
            .Append(". ")
            .Append(KindClause(last.InferredKind));
        return sb.ToString();
    }

    private static string AgoPhrase(DateTime detectedAtUtc, DateTime utcNow)
    {
        var mins = (int)Math.Floor((utcNow - detectedAtUtc).TotalMinutes);
        if (mins < 0) mins = 0;
        if (mins < 2) return "przed chwilą";
        return PolishNumberWords.MinutesPhrase(mins) + " temu";
    }

    private static string LookbackPhrase(TimeSpan lookback)
    {
        var days = (int)Math.Round(lookback.TotalDays);
        if (days <= 0) days = 1;
        return PolishNumberWords.DaysPhrase(days);
    }

    private static string FormatLocalDateTime(DateTime utc, TimeZoneInfo tz, CultureInfo pl)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
        return local.ToString("g", pl);
    }

    private static string KindClause(string inferredKind) =>
        inferredKind switch
        {
            "likelyRain" => "Heurystyka: kilka czujników naraz — możliwy deszcz, niekoniecznie podlewanie. ",
            "likelyManual" => "Heurystyka: raczej podlanie jednego obszaru. ",
            _ => string.Empty,
        };
}
