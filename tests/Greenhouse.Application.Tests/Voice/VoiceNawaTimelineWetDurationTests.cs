using Greenhouse.Application.Voice;
using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Application.Tests.Voice;

public sealed class VoiceNawaTimelineWetDurationTests
{
    [Fact]
    public void ContinuousTooWet_ReturnsMinutesSinceLastDry_BelowThreshold()
    {
        var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var s1 = Guid.NewGuid();

        var readings = new List<SensorReading>
        {
            SensorReading.Create("a", now.AddMinutes(-40), "t", "{}", 50m, null, null, null, s1),
            SensorReading.Create("a", now.AddMinutes(-10), "t", "{}", 80m, null, null, null, s1),
        };

        var per = VoiceNawaTimeline.BuildPerSensorLists(readings, [s1]);
        var minutes = VoiceNawaTimeline.EstimateContinuousTooWetMinutesFromNow(per, [s1], 70m, now, maxLookbackMinutes: 120);

        Assert.Equal(11, minutes);
    }

    [Fact]
    public void ContinuousTooWet_AtLeast30Minutes_WhenDryWasLongAgo()
    {
        var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var s1 = Guid.NewGuid();

        var readings = new List<SensorReading>
        {
            SensorReading.Create("a", now.AddMinutes(-90), "t", "{}", 50m, null, null, null, s1),
            SensorReading.Create("a", now.AddMinutes(-35), "t", "{}", 80m, null, null, null, s1),
        };

        var per = VoiceNawaTimeline.BuildPerSensorLists(readings, [s1]);
        var minutes = VoiceNawaTimeline.EstimateContinuousTooWetMinutesFromNow(per, [s1], 70m, now, maxLookbackMinutes: 120);

        Assert.True(minutes >= 30);
    }

    [Fact]
    public void ContinuousTooWet_ReturnsZero_WhenNotWetAtNow()
    {
        var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var s1 = Guid.NewGuid();
        var readings = new List<SensorReading>
        {
            SensorReading.Create("a", now.AddMinutes(-5), "t", "{}", 50m, null, null, null, s1),
        };

        var per = VoiceNawaTimeline.BuildPerSensorLists(readings, [s1]);
        var minutes = VoiceNawaTimeline.EstimateContinuousTooWetMinutesFromNow(per, [s1], 70m, now, maxLookbackMinutes: 120);

        Assert.Equal(0, minutes);
    }
}
