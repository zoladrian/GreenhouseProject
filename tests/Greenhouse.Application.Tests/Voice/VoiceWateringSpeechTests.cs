using System.Globalization;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Voice;

namespace Greenhouse.Application.Tests.Voice;

public sealed class VoiceWateringSpeechTests
{
    private static readonly TimeZoneInfo Tz =
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Warsaw");

    private static readonly CultureInfo Pl = new("pl-PL");

    private static readonly DateTime UtcNow = new(2026, 4, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ForPostWatering_WhenNoEvent_ShouldReturnLookbackPhrase()
    {
        var text = VoiceWateringSpeech.ForPostWateringContext(
            last: null,
            utcNow: UtcNow,
            tz: Tz,
            pl: Pl,
            wateringLookback: TimeSpan.FromDays(30));

        Assert.Contains("trzydzieści dni", text);
        Assert.Contains("nie znaleziono", text);
    }

    [Fact]
    public void ForDrySinceWatering_WhenNoEvent_ShouldReturnLookbackPhrase()
    {
        var text = VoiceWateringSpeech.ForDrySinceWatering(
            last: null,
            utcNow: UtcNow,
            tz: Tz,
            pl: Pl,
            wateringLookback: TimeSpan.FromDays(7));

        Assert.Contains("siedem dni", text);
        Assert.Contains("nie podam", text);
    }

    [Fact]
    public void ForPostWatering_RecentEvent_ShouldSay_PrzedChwila()
    {
        // 1 minuta temu = "przed chwilą" (pomijamy formę liczbową dla bardzo krótkich okresów).
        var ev = MakeEvent(UtcNow.AddMinutes(-1), "likelyManual");

        var text = VoiceWateringSpeech.ForPostWateringContext(ev, UtcNow, Tz, Pl, TimeSpan.FromDays(30));

        Assert.Contains("przed chwilą", text);
        Assert.Contains("podlanie jednego obszaru", text);
    }

    [Fact]
    public void ForPostWatering_EventOlderThanTwoMinutes_ShouldUseMinutesPhrase()
    {
        var ev = MakeEvent(UtcNow.AddMinutes(-15), "likelyRain");

        var text = VoiceWateringSpeech.ForPostWateringContext(ev, UtcNow, Tz, Pl, TimeSpan.FromDays(30));

        Assert.Contains("piętnaście minut temu", text);
        Assert.Contains("możliwy deszcz", text);
    }

    [Fact]
    public void ForDrySinceWatering_ShouldVocalizeMinutesElapsed()
    {
        var ev = MakeEvent(UtcNow.AddMinutes(-5), "unknown");

        var text = VoiceWateringSpeech.ForDrySinceWatering(ev, UtcNow, Tz, Pl, TimeSpan.FromDays(30));

        Assert.Contains("pięć minut", text);
    }

    [Fact]
    public void ForDrySinceWatering_FutureTimestamp_ShouldClampToZero()
    {
        // Bezpieczeństwo: jeśli zegar source jest przesunięty, NIE liczymy minus minut.
        var ev = MakeEvent(UtcNow.AddMinutes(5), "likelyManual");

        var text = VoiceWateringSpeech.ForDrySinceWatering(ev, UtcNow, Tz, Pl, TimeSpan.FromDays(30));

        Assert.Contains("zero minut", text);
    }

    [Fact]
    public void ForPostWatering_ShouldFormatLocalDateTime_InPolishLocale()
    {
        // Środa 25 kwietnia 2026 12:00 UTC = 14:00 lokalnie (CEST).
        var ev = MakeEvent(UtcNow, "likelyManual");

        var text = VoiceWateringSpeech.ForPostWateringContext(ev, UtcNow, Tz, Pl, TimeSpan.FromDays(30));

        Assert.Contains("25.04.2026", text);
    }

    [Fact]
    public void LookbackPhrase_WhenZeroOrNegative_ShouldFallbackToOneDay()
    {
        var text = VoiceWateringSpeech.ForPostWateringContext(
            last: null,
            utcNow: UtcNow,
            tz: Tz,
            pl: Pl,
            wateringLookback: TimeSpan.Zero);

        Assert.Contains("jeden dzień", text);
    }

    [Theory]
    [InlineData("likelyRain", "możliwy deszcz")]
    [InlineData("likelyManual", "podlanie jednego obszaru")]
    [InlineData("unknown", "")]
    [InlineData("", "")]
    public void KindClause_ShouldMapInferredKind(string inferred, string expectedFragment)
    {
        var ev = MakeEvent(UtcNow.AddMinutes(-3), inferred);

        var text = VoiceWateringSpeech.ForPostWateringContext(ev, UtcNow, Tz, Pl, TimeSpan.FromDays(30));

        if (expectedFragment.Length == 0)
        {
            Assert.DoesNotContain("podlanie jednego obszaru", text);
            Assert.DoesNotContain("możliwy deszcz", text);
        }
        else
        {
            Assert.Contains(expectedFragment, text);
        }
    }

    private static WateringEventDto MakeEvent(DateTime at, string kind) =>
        new(
            DetectedAtUtc: at,
            MoistureBefore: 30m,
            MoistureAfter: 50m,
            DeltaMoisture: 20m,
            WindowDuration: TimeSpan.FromMinutes(3),
            InferredKind: kind,
            ContributingSensorCount: 1);
}
