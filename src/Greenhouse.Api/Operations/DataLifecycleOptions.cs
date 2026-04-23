using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Api.Operations;

public sealed class DataLifecycleOptions
{
    public const string SectionName = "DataLifecycle";

    public bool EnablePruning { get; init; } = true;

    [Range(1, 3650)]
    public int KeepReadingsDays { get; init; } = 180;

    [Range(1, 168)]
    public int PruneIntervalHours { get; init; } = 12;
}
