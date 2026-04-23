using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Application.Voice;

/// <summary>Konfiguracja powitania głosowego (offline, bez internetu).</summary>
public sealed class VoiceOptions
{
    public const string SectionName = "Voice";

    /// <summary>Początek wypowiedzi, np. &quot;Dzień dobry Panie Czesławie&quot;.</summary>
    [Required]
    [MinLength(2)]
    public string GreetingLeadin { get; init; } = "Dzień dobry";

    /// <summary>Strefa do początku „dzisiaj” (średnie od lokalnej północy).</summary>
    [Required]
    [MinLength(3)]
    public string TimeZoneId { get; init; } = "Europe/Warsaw";
}
