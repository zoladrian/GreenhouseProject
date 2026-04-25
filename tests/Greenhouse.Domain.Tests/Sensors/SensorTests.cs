using Greenhouse.Domain.Sensors;

namespace Greenhouse.Domain.Tests.Sensors;

public sealed class SensorTests
{
    [Fact]
    public void Register_ShouldTrimExternalId()
    {
        var s = Sensor.Register("  id-1  ");
        Assert.Equal("id-1", s.ExternalId);
        Assert.Null(s.NawaId);
    }

    [Fact]
    public void AssignToNawa_ThenUnassign_ShouldClearNawa()
    {
        var s = Sensor.Register("s1");
        var nawaId = Guid.NewGuid();
        s.AssignToNawa(nawaId);
        Assert.Equal(nawaId, s.NawaId);
        s.UnassignFromNawa();
        Assert.Null(s.NawaId);
    }

    [Fact]
    public void AssignToNawa_ShouldRejectEmptyGuid()
    {
        var s = Sensor.Register("s1");
        Assert.Throws<ArgumentException>(() => s.AssignToNawa(Guid.Empty));
    }

    [Fact]
    public void RekeyExternalId_ShouldReplaceStableKey()
    {
        var s = Sensor.Register("stary_topic_name");
        s.RekeyExternalId("0x00158d0001a2b3c4");
        Assert.Equal("0x00158d0001a2b3c4", s.ExternalId);
    }

    [Fact]
    public void RekeyExternalId_ShouldRejectEmpty()
    {
        var s = Sensor.Register("x");
        Assert.Throws<ArgumentException>(() => s.RekeyExternalId("  "));
    }

    [Fact]
    public void AssignToNawa_ShouldReject_WhenSensorKindIsWeather()
    {
        var s = Sensor.Register("rain-1", SensorKind.Weather);
        var ex = Assert.Throws<InvalidOperationException>(() => s.AssignToNawa(Guid.NewGuid()));
        Assert.Contains("global", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateKind_ToWeather_ShouldUnassignFromNawa()
    {
        var nawaId = Guid.NewGuid();
        var s = Sensor.Register("plant", SensorKind.Soil);
        s.AssignToNawa(nawaId);
        s.UpdateKind(SensorKind.Weather);
        Assert.Null(s.NawaId);
        Assert.Equal(SensorKind.Weather, s.Kind);
    }
}
