using Greenhouse.Application.Charts;
using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Tests.Charts;

public sealed class NawaChartSensorScopeTests
{
    private static readonly Guid Nawa1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Nawa2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void MoistureEnvironment_IncludesOnlyNonWeatherSensorsAssignedToNawa()
    {
        var soilOnNawa1 = Sensor.Register("soil-1", SensorKind.Soil);
        soilOnNawa1.AssignToNawa(Nawa1);
        var soilOnNawa2 = Sensor.Register("soil-2", SensorKind.Soil);
        soilOnNawa2.AssignToNawa(Nawa2);
        var rainGlobal = Sensor.Register("rain-1", SensorKind.Weather);

        var sut = new NawaChartSensorScope(new[] { soilOnNawa1, soilOnNawa2, rainGlobal });

        var ids = sut.ResolveMoistureEnvironmentAndGlobalWeatherSensorIds(Nawa1);

        Assert.Contains(soilOnNawa1.Id, ids);
        Assert.DoesNotContain(rainGlobal.Id, ids);
        Assert.DoesNotContain(soilOnNawa2.Id, ids);
        Assert.Single(ids);
    }

    [Fact]
    public void GlobalWeatherSeries_OnlyKindWeatherWithNullNawa()
    {
        var g1 = Sensor.Register("g1", SensorKind.Weather);
        var soil = Sensor.Register("s", SensorKind.Soil);
        soil.AssignToNawa(Nawa1);

        var sut = new NawaChartSensorScope(new[] { g1, soil });

        var ids = sut.ResolveGlobalWeatherSensorIds();

        Assert.Single(ids);
        Assert.Equal(g1.Id, ids[0]);
    }
}
