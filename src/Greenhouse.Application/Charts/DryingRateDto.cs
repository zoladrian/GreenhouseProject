namespace Greenhouse.Application.Charts;

public sealed record DryingRateDto(
    string SensorIdentifier,
    Guid? SensorId,
    DateTime WindowStart,
    DateTime WindowEnd,
    decimal PercentPerHour);
