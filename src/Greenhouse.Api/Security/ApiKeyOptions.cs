using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Api.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiSecurity";

    public bool RequireForMutations { get; init; } = true;

    [Required]
    [MinLength(8)]
    public string ApiKey { get; init; } = "change-me-greenhouse";

    [Required]
    [MinLength(3)]
    public string HeaderName { get; init; } = "X-Api-Key";
}
