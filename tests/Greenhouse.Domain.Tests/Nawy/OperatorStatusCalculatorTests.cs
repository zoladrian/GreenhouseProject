using Greenhouse.Domain.Nawy;

namespace Greenhouse.Domain.Tests.Nawy;

public sealed class OperatorStatusCalculatorTests
{
    private static readonly DateTime Now = new(2026, 3, 19, 21, 0, 0, DateTimeKind.Utc);

    private static OperatorStatus Calc(
        int assigned,
        int moistureN,
        decimal? minM,
        decimal? maxM,
        DateTime? oldest,
        MoistureThresholds th,
        decimal spreadAlert = OperatorStatusCalculator.DefaultSpreadAlertPercent) =>
        OperatorStatusCalculator.Calculate(assigned, moistureN, minM, maxM, oldest, th, Now, spreadAlert);

    [Fact]
    public void NoAssignedSensors_ReturnsNoData()
    {
        Assert.Equal(OperatorStatus.NoData, Calc(0, 0, null, null, null, MoistureThresholds.Empty));
    }

    [Fact]
    public void AssignedButNoMoistureReadings_ReturnsNoData()
    {
        Assert.Equal(OperatorStatus.NoData, Calc(2, 0, null, null, Now.AddMinutes(-5), new MoistureThresholds(20m, 80m)));
    }

    [Fact]
    public void StaleData_ReturnsNoData()
    {
        var oldReading = Now - TimeSpan.FromHours(2);
        Assert.Equal(OperatorStatus.NoData, Calc(1, 1, 50m, 50m, oldReading, MoistureThresholds.Empty));
    }

    [Fact]
    public void SingleSensor_BelowMin_ReturnsDry()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(20m, 80m);
        Assert.Equal(OperatorStatus.Dry, Calc(1, 1, 15m, 15m, recent, th));
    }

    [Fact]
    public void SingleSensor_AboveMax_ReturnsWarning()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(20m, 80m);
        Assert.Equal(OperatorStatus.Warning, Calc(1, 1, 90m, 90m, recent, th));
    }

    [Fact]
    public void SingleSensor_InRange_ReturnsOk()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(20m, 80m);
        Assert.Equal(OperatorStatus.Ok, Calc(1, 1, 50m, 50m, recent, th));
    }

    [Fact]
    public void NoThresholds_SingleSensor_ReturnsOk()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        Assert.Equal(OperatorStatus.Ok, Calc(1, 1, 50m, 50m, recent, MoistureThresholds.Empty));
    }

    [Fact]
    public void NoThresholds_TwoSensors_LargeSpread_ReturnsUneven()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        Assert.Equal(
            OperatorStatus.UnevenMoisture,
            Calc(2, 2, 30m, 60m, recent, MoistureThresholds.Empty, spreadAlert: 15m));
    }

    [Fact]
    public void NoThresholds_TwoSensors_SmallSpread_ReturnsOk()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        Assert.Equal(OperatorStatus.Ok, Calc(2, 2, 48m, 52m, recent, MoistureThresholds.Empty, spreadAlert: 15m));
    }

    [Fact]
    public void TwoSensors_MinDry_MaxWet_ReturnsConflict()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(30m, 70m);
        Assert.Equal(OperatorStatus.Conflict, Calc(2, 2, 10m, 90m, recent, th));
    }

    [Fact]
    public void TwoSensors_OnlyMinTooDry_ReturnsDry()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(30m, 70m);
        Assert.Equal(OperatorStatus.Dry, Calc(2, 2, 10m, 50m, recent, th));
    }

    [Fact]
    public void TwoSensors_OnlyMaxTooWet_ReturnsWarning()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(30m, 70m);
        Assert.Equal(OperatorStatus.Warning, Calc(2, 2, 50m, 85m, recent, th));
    }

    [Fact]
    public void TwoSensors_InBandButLargeSpread_ReturnsUneven()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(20m, 80m);
        Assert.Equal(OperatorStatus.UnevenMoisture, Calc(2, 2, 35m, 55m, recent, th, spreadAlert: 15m));
    }

    [Fact]
    public void TwoSensors_InBandSmallSpread_ReturnsOk()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(20m, 80m);
        Assert.Equal(OperatorStatus.Ok, Calc(2, 2, 48m, 52m, recent, th, spreadAlert: 15m));
    }

    [Fact]
    public void Conflict_TakesPriority_OverUneven()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var th = new MoistureThresholds(40m, 60m);
        Assert.Equal(OperatorStatus.Conflict, Calc(2, 2, 5m, 95m, recent, th, spreadAlert: 10m));
    }
}
