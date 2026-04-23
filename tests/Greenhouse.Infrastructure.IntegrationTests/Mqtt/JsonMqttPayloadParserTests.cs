using Greenhouse.Infrastructure.Mqtt;

namespace Greenhouse.Infrastructure.IntegrationTests.Mqtt;

public sealed class JsonMqttPayloadParserTests
{
    private readonly JsonMqttPayloadParser _sut = new();

    [Fact]
    public void ParseSensorPayload_ShouldReadIeee_FromDeviceObject()
    {
        const string json = """
            {
              "soil_moisture": 55,
              "battery": 100,
              "linkquality": 200,
              "device": {
                "friendlyName": "Koło balkonu",
                "ieeeAddr": "0xffffb40e0605c41c"
              }
            }
            """;

        var r = _sut.ParseSensorPayload(json);

        Assert.Equal(55m, r.SoilMoisture);
        Assert.Equal("0xffffb40e0605c41c", r.IeeeAddress);
    }

    [Fact]
    public void ParseSensorPayload_ShouldReadIeee_FromRoot_ieee_address()
    {
        const string json = """{"soil_moisture":1,"ieee_address":"0x00158d0001a2b3c4"}""";

        var r = _sut.ParseSensorPayload(json);

        Assert.Equal("0x00158d0001a2b3c4", r.IeeeAddress);
    }

    [Fact]
    public void ParseSensorPayload_ShouldReadWeatherFields_ForRainSensor()
    {
        const string json = """
            {
              "rain": true,
              "rain_intensity": 17,
              "battery": 92,
              "illuminance_raw": 545,
              "illuminance_average_20min": 430,
              "illuminance_maximum_today": 1120,
              "cleaning_reminder": "ON",
              "linkquality": 180
            }
            """;

        var r = _sut.ParseSensorPayload(json);

        Assert.True(r.Rain);
        Assert.Equal(17m, r.RainIntensityRaw);
        Assert.Equal(545m, r.IlluminanceRaw);
        Assert.Equal(430m, r.IlluminanceAverage20MinRaw);
        Assert.Equal(1120m, r.IlluminanceMaximumTodayRaw);
        Assert.True(r.CleaningReminder);
        Assert.Equal(92, r.Battery);
        Assert.Equal(180, r.LinkQuality);
    }
}
