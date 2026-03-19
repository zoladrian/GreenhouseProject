using Greenhouse.Domain.Nawy;

namespace Greenhouse.Domain.Tests.Nawy;

public sealed class NawaThresholdsTests
{
    [Fact]
    public void SetPlantNote_ShouldTrimAndStore()
    {
        var nawa = Nawa.Create("Test", null);
        nawa.SetPlantNote("  Pomidory  ");
        Assert.Equal("Pomidory", nawa.PlantNote);
    }

    [Fact]
    public void SetPlantNote_NullClears()
    {
        var nawa = Nawa.Create("Test", null);
        nawa.SetPlantNote("Pomidory");
        nawa.SetPlantNote(null);
        Assert.Null(nawa.PlantNote);
    }

    [Fact]
    public void SetPlantNote_TruncatesAt200()
    {
        var nawa = Nawa.Create("Test", null);
        nawa.SetPlantNote(new string('a', 250));
        Assert.Equal(200, nawa.PlantNote!.Length);
    }

    [Fact]
    public void UpdateMoistureThresholds_ShouldSet()
    {
        var nawa = Nawa.Create("Test", null);
        nawa.UpdateMoistureThresholds(20m, 80m);
        Assert.Equal(20m, nawa.MoistureMin);
        Assert.Equal(80m, nawa.MoistureMax);
    }

    [Fact]
    public void UpdateMoistureThresholds_ShouldRejectMinGtMax()
    {
        var nawa = Nawa.Create("Test", null);
        Assert.Throws<ArgumentException>(() => nawa.UpdateMoistureThresholds(80m, 20m));
    }

    [Fact]
    public void UpdateTemperatureThresholds_ShouldSet()
    {
        var nawa = Nawa.Create("Test", null);
        nawa.UpdateTemperatureThresholds(10m, 35m);
        Assert.Equal(10m, nawa.TemperatureMin);
        Assert.Equal(35m, nawa.TemperatureMax);
    }

    [Fact]
    public void MoistureThresholds_IsBelowMin_ShouldDetect()
    {
        var thresholds = new MoistureThresholds(20m, 80m);
        Assert.True(thresholds.IsBelowMin(15m));
        Assert.False(thresholds.IsBelowMin(25m));
    }
}
