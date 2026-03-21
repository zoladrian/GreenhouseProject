namespace Greenhouse.Application.Abstractions;

/// <summary>
/// Scala duplikaty czujników (stary wpis po nazwie z topicu + wpis po IEEE) i rekeyuje osierocone rekordy na podstawie JSON w odczytach.
/// </summary>
public interface ISensorDuplicateMerger
{
    Task<SensorDuplicateMergeResult> MergeAsync(CancellationToken cancellationToken);
}

public sealed record SensorDuplicateMergeResult(int MergedLegacySensors, int RekeyedLegacySensors);
