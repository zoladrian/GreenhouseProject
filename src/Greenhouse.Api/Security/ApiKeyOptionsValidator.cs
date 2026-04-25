using Microsoft.Extensions.Options;

namespace Greenhouse.Api.Security;

/// <summary>
/// Wymusza fail-fast: gdy <see cref="ApiKeyOptions.RequireForMutations"/> = <c>true</c>,
/// wartość <see cref="ApiKeyOptions.ApiKey"/> musi istnieć, mieć min. 16 znaków
/// i NIE może być jednym ze znanych placeholderów typu "change-me-*".
/// </summary>
public sealed class ApiKeyOptionsValidator : IValidateOptions<ApiKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiKeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.RequireForMutations)
        {
            return ValidateOptionsResult.Success;
        }

        var key = options.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return ValidateOptionsResult.Fail(
                "ApiSecurity:ApiKey jest wymagany, gdy ApiSecurity:RequireForMutations=true. " +
                "Ustaw zmienną środowiskową ApiSecurity__ApiKey (np. w .env / docker-compose) " +
                "lub wyłącz RequireForMutations w środowisku deweloperskim.");
        }

        if (key.Length < ApiKeyOptions.MinimumApiKeyLength)
        {
            return ValidateOptionsResult.Fail(
                $"ApiSecurity:ApiKey musi mieć co najmniej {ApiKeyOptions.MinimumApiKeyLength} znaków. " +
                "Wygeneruj losowy sekret (np. `openssl rand -hex 24`).");
        }

        var lowered = key.ToLowerInvariant();
        foreach (var placeholder in ApiKeyOptions.DisallowedPlaceholders)
        {
            if (lowered.Contains(placeholder, StringComparison.Ordinal))
            {
                return ValidateOptionsResult.Fail(
                    $"ApiSecurity:ApiKey wygląda na placeholder ('{placeholder}'). " +
                    "Wygeneruj prawdziwy sekret i przekaż go przez zmienną środowiskową.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
