using Greenhouse.Domain.Nawy;

namespace Greenhouse.Domain.Tests.Nawy;

public sealed class OperatorStatusCalculatorTests
{
    private static readonly DateTime Now = new(2026, 3, 19, 21, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NoSensors_ReturnsNoData()
    {
        var result = OperatorStatusCalculator.Calculate(0, null, null, MoistureThresholds.Empty, Now);
        Assert.Equal(OperatorStatus.NoData, result);
    }

    [Fact]
    public void StaleData_ReturnsNoData()
    {
        var oldReading = Now - TimeSpan.FromHours(2);
        var result = OperatorStatusCalculator.Calculate(1, 50m, oldReading, MoistureThresholds.Empty, Now);
        Assert.Equal(OperatorStatus.NoData, result);
    }

    [Fact]
    public void FreshData_BelowMin_ReturnsDry()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var thresholds = new MoistureThresholds(20m, 80m);
        var result = OperatorStatusCalculator.Calculate(2, 15m, recent, thresholds, Now);
        Assert.Equal(OperatorStatus.Dry, result);
    }

    [Fact]
    public void FreshData_AboveMax_ReturnsWarning()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var thresholds = new MoistureThresholds(20m, 80m);
        var result = OperatorStatusCalculator.Calculate(2, 90m, recent, thresholds, Now);
        Assert.Equal(OperatorStatus.Warning, result);
    }

    [Fact]
    public void FreshData_InRange_ReturnsOk()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var thresholds = new MoistureThresholds(20m, 80m);
        var result = OperatorStatusCalculator.Calculate(2, 50m, recent, thresholds, Now);
        Assert.Equal(OperatorStatus.Ok, result);
    }

    [Fact]
    public void FreshData_NoThresholds_ReturnsOk()
    {
        var recent = Now - TimeSpan.FromMinutes(5);
        var result = OperatorStatusCalculator.Calculate(2, 50m, recent, MoistureThresholds.Empty, Now);
        Assert.Equal(OperatorStatus.Ok, result);
    }
}
