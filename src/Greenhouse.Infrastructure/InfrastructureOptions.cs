using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Infrastructure;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    [Required]
    [MinLength(3)]
    public string DatabasePath { get; init; } = "data/greenhouse.db";
}
