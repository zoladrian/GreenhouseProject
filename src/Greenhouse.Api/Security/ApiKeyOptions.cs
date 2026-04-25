using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Api.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiSecurity";

    public const int MinimumApiKeyLength = 16;

    internal static readonly string[] DisallowedPlaceholders =
    {
        "change-me",
        "dev-only",
        "your-key",
        "secret",
        "api-key",
        "todo",
    };

    public bool RequireForMutations { get; init; } = true;

    public string? ApiKey { get; init; }

    [Required]
    [MinLength(3)]
    public string HeaderName { get; init; } = "X-Api-Key";
}
