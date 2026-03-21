using Greenhouse.Domain.Nawy;

namespace Greenhouse.Application.Nawy;

public sealed record NawaSnapshotDto(
    Guid NawaId,
    string NawaName,
    string? PlantNote,
    OperatorStatus Status,
    int SensorCount,
    /// <summary>Liczba czujników z ostatnim odczytem wilgotności (do agregacji min/max).</summary>
    int MoistureReadingCount,
    decimal? AvgMoisture,
    decimal? MinMoisture,
    decimal? MaxMoisture,
    /// <summary>Max − min wilgotności (%); null jeśli &lt; 2 odczytów.</summary>
    decimal? MoistureSpread,
    decimal? AvgTemperature,
    int? LowestBattery,
    DateTime? OldestReadingUtc,
    DateTime GeneratedAtUtc,
    decimal? MoistureMin,
    decimal? MoistureMax,
    decimal? TemperatureMin,
    decimal? TemperatureMax,
    /// <summary>Krótka wypowiedź o ostatnim podlaniu (TTS), gdy status sucho / konflikt / po podlaniu.</summary>
    string? WateringSpeechNote);
