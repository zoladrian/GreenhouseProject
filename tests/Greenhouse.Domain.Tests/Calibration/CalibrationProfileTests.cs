using Greenhouse.Domain.Calibration;

namespace Greenhouse.Domain.Tests.Calibration;

public sealed class CalibrationProfileTests
{
    [Fact]
    public void Create_ShouldSetFields()
    {
        var profile = CalibrationProfile.Create("Default", 200m, 800m);
        Assert.Equal("Default", profile.Name);
        Assert.Equal(200m, profile.DryRawValue);
        Assert.Equal(800m, profile.WetRawValue);
    }

    [Fact]
    public void Create_ShouldRejectDryGteWet()
    {
        Assert.Throws<ArgumentException>(() => CalibrationProfile.Create("Bad", 800m, 200m));
    }

    [Fact]
    public void CalibrateToPercent_ShouldConvertCorrectly()
    {
        var profile = CalibrationProfile.Create("Test", 200m, 800m);
        Assert.Equal(50m, profile.CalibrateToPercent(500m));
        Assert.Equal(0m, profile.CalibrateToPercent(100m));
        Assert.Equal(100m, profile.CalibrateToPercent(900m));
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(800, 100)]
    [InlineData(500, 50)]
    public void CalibrateToPercent_EdgeCases(decimal raw, decimal expected)
    {
        var profile = CalibrationProfile.Create("Test", 200m, 800m);
        Assert.Equal(expected, profile.CalibrateToPercent(raw));
    }
}
