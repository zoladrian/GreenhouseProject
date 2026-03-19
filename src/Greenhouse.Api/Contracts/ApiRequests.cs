namespace Greenhouse.Api.Contracts;

public sealed record CreateNawaRequest(string Name, string? Description);

public sealed record UpdateNawaRequest(
    string Name,
    string? Description,
    string? PlantNote,
    bool IsActive,
    decimal? MoistureMin,
    decimal? MoistureMax,
    decimal? TemperatureMin,
    decimal? TemperatureMax);

/// <summary>
/// null = odłącz od nawy; guid = przypisz do nawy.
/// </summary>
public sealed record AssignSensorNawaRequest(Guid? NawaId);

public sealed record UpdateSensorDisplayNameRequest(string? DisplayName);
