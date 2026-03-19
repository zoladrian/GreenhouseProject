namespace Greenhouse.Domain.Nawy;

public sealed class NawaSnapshot
{
    public Guid NawaId { get; }
    public string NawaName { get; }
    public string? PlantNote { get; }
    public OperatorStatus Status { get; }
    public int SensorCount { get; }
    public decimal? AvgMoisture { get; }
    public decimal? MinMoisture { get; }
    public decimal? MaxMoisture { get; }
    public decimal? AvgTemperature { get; }
    public int? LowestBattery { get; }
    public DateTime? OldestReadingUtc { get; }
    public DateTime GeneratedAtUtc { get; }

    public NawaSnapshot(
        Guid nawaId,
        string nawaName,
        string? plantNote,
        OperatorStatus status,
        int sensorCount,
        decimal? avgMoisture,
        decimal? minMoisture,
        decimal? maxMoisture,
        decimal? avgTemperature,
        int? lowestBattery,
        DateTime? oldestReadingUtc,
        DateTime generatedAtUtc)
    {
        NawaId = nawaId;
        NawaName = nawaName;
        PlantNote = plantNote;
        Status = status;
        SensorCount = sensorCount;
        AvgMoisture = avgMoisture;
        MinMoisture = minMoisture;
        MaxMoisture = maxMoisture;
        AvgTemperature = avgTemperature;
        LowestBattery = lowestBattery;
        OldestReadingUtc = oldestReadingUtc;
        GeneratedAtUtc = generatedAtUtc;
    }
}
