namespace Greenhouse.Application.Nawy;

public sealed record NawaDto(
    Guid Id,
    string Name,
    string? Description,
    string? PlantNote,
    bool IsActive,
    decimal? MoistureMin,
    decimal? MoistureMax,
    decimal? TemperatureMin,
    decimal? TemperatureMax,
    DateTime CreatedAtUtc);
