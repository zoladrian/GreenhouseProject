namespace Greenhouse.Application.Voice;

/// <summary>Konfiguracja powitania głosowego (offline, bez internetu).</summary>
public sealed class VoiceOptions
{
    public const string SectionName = "Voice";

    /// <summary>Początek wypowiedzi, np. &quot;Dzień dobry Panie Czesławie&quot;.</summary>
    public string GreetingLeadin { get; init; } = "Dzień dobry";

    /// <summary>Strefa do początku „dzisiaj” (średnie od lokalnej północy).</summary>
    public string TimeZoneId { get; init; } = "Europe/Warsaw";
}
