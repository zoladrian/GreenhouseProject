namespace Greenhouse.Application.Abstractions;

/// <summary>Wynik zapewnienia rekordu czujnika w bazie.</summary>
public readonly record struct SensorEnsureResult(Guid SensorId, bool CreatedNew);
