using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Api.Charts;

public sealed class ChartQueryOptions
{
    public const string SectionName = "Charts";

    [Range(10, 100_000)]
    public int MaxPointsPerSeries { get; init; } = 5000;
}
