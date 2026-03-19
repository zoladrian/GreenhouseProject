namespace Greenhouse.Application.Sensors;

public sealed record SensorListItemDto(
    Guid Id,
    string ExternalId,
    string? DisplayName,
    Guid? NawaId,
    DateTime CreatedAtUtc);
