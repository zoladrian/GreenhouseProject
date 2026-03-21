namespace Greenhouse.Application.Charts;

/// <param name="SensorIdentifier">Etykieta do UI: <c>DisplayName</c> czujnika lub <c>ExternalId</c>.</param>
public sealed record DryingRateDto(
    string SensorIdentifier,
    Guid? SensorId,
    DateTime WindowStart,
    DateTime WindowEnd,
    decimal PercentPerHour);
