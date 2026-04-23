using Greenhouse.Domain.SensorReadings;

namespace Greenhouse.Domain.Tests.SensorReadings;

public sealed class SensorReadingTests
{
    [Fact]
    public void Create_ShouldSetAllFields()
    {
        var reading = SensorReading.Create(
            "sensor-1",
            new DateTime(2026, 3, 19, 20, 0, 0, DateTimeKind.Utc),
            "zigbee2mqtt/sensor-1",
            "{\"soil_moisture\":40}",
            40m,
            22.5m,
            100,
            255,
            true,
            12m,
            500m,
            450m,
            980m,
            false);

        Assert.Equal("sensor-1", reading.SensorIdentifier);
        Assert.Equal(40m, reading.SoilMoisture);
        Assert.Equal(22.5m, reading.Temperature);
        Assert.Equal(100, reading.Battery);
        Assert.Equal(255, reading.LinkQuality);
        Assert.True(reading.Rain);
        Assert.Equal(12m, reading.RainIntensityRaw);
        Assert.Equal(500m, reading.IlluminanceRaw);
        Assert.Equal(450m, reading.IlluminanceAverage20MinRaw);
        Assert.Equal(980m, reading.IlluminanceMaximumTodayRaw);
        Assert.False(reading.CleaningReminder);
        Assert.Null(reading.SensorId);
    }

    [Fact]
    public void Create_ShouldSetSensorId_WhenProvided()
    {
        var sid = Guid.NewGuid();
        var reading = SensorReading.Create(
            "sensor-1",
            DateTime.UtcNow,
            "zigbee2mqtt/sensor-1",
            "{}",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            sid);

        Assert.Equal(sid, reading.SensorId);
    }
}
