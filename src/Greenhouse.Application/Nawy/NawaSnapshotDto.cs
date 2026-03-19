using Greenhouse.Domain.Nawy;

namespace Greenhouse.Application.Nawy;

public sealed record NawaSnapshotDto(
    Guid NawaId,
    string NawaName,
    string? PlantNote,
    OperatorStatus Status,
    int SensorCount,
    decimal? AvgMoisture,
    decimal? MinMoisture,
    decimal? MaxMoisture,
    decimal? AvgTemperature,
    int? LowestBattery,
    DateTime? OldestReadingUtc,
    DateTime GeneratedAtUtc);
