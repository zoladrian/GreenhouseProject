using System.Globalization;
using System.Text;
using Greenhouse.Application.Charts;

namespace Greenhouse.Application.Voice;

/// <summary>
/// Zdania głosowe o ostatnim wykrytym podlaniu (heurystyka skoku wilgotności).
/// </summary>
internal static class VoiceWateringSpeech
{
    /// <summary>Okno wstecz, w którym szukamy skoków — spójne z briefem i raportem.</summary>
    public static readonly TimeSpan DefaultWateringLookback = TimeSpan.FromDays(30);

    public static string ForPostWateringContext(WateringEventDto? last, DateTime utcNow, TimeZoneInfo tz, CultureInfo pl)
    {
        if (last is null)
        {
            return "W ostatnich trzydziestu dniach nie znaleziono wyraźnego skoku wilgotności jak po podlaniu — dokładnej daty ostatniego podlania nie podam. ";
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

    public static string ForDrySinceWatering(WateringEventDto? last, DateTime utcNow, TimeZoneInfo tz, CultureInfo pl)
    {
        if (last is null)
        {
            return "W ostatnich trzydziestu dniach nie wykryłem wyraźnego skoku wilgotności związanego z podlaniem — nie podam, ile minut minęło od podlania. ";
        }

        var mins = (int)Math.Floor((utcNow - last.DetectedAtUtc).TotalMinutes);
        if (mins < 0)
            mins = 0;

        var sb = new StringBuilder();
        sb.Append("Od ostatniego wykrytego podlania lub silnego zawilgocenia minęło ")
            .Append(MinutesNominative(mins))
            .Append(". To było około ")
            .Append(FormatLocalDateTime(last.DetectedAtUtc, tz, pl))
            .Append(". ")
            .Append(KindClause(last.InferredKind));
        return sb.ToString();
    }

    private static string AgoPhrase(DateTime detectedAtUtc, DateTime utcNow)
    {
        var mins = (int)Math.Floor((utcNow - detectedAtUtc).TotalMinutes);
        if (mins < 0)
            mins = 0;
        if (mins < 2)
            return "przed chwilą";

        return MinutesNominative(mins) + " temu";
    }

    /// <summary>Mianownik: „pięć minut”, „jedna minuta”.</summary>
    private static string MinutesNominative(int n)
    {
        if (n == 0)
            return "zero minut";
        if (n == 1)
            return "jedna minuta";

        var mod10 = n % 10;
        var mod100 = n % 100;
        var word = mod10 == 1 && mod100 != 11
            ? "minuta"
            : mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)
                ? "minuty"
                : "minut";

        return $"{n.ToString(CultureInfo.InvariantCulture)} {word}";
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
