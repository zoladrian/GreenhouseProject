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
}
