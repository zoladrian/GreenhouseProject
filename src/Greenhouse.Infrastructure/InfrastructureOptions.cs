namespace Greenhouse.Infrastructure;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public string DatabasePath { get; init; } = "data/greenhouse.db";
}
