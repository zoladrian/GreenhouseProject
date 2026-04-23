namespace Greenhouse.Application.Sensors;

public sealed record SensorListItemDto(
    Guid Id,
    string ExternalId,
    string? DisplayName,
    string Kind,
    Guid? NawaId,
    DateTime CreatedAtUtc);
