using System.ComponentModel.DataAnnotations;

namespace Greenhouse.Application.Voice;

/// <summary>
/// Konfigurowalne stałe dla heurystyk analitycznych (post-watering, lookback dla wykrywania
/// podlewania). Zastępuje magiczne liczby rozsiane po kodzie (240 minut, 30 dni).
///
/// Sekcja konfiguracji: <c>"Analytics"</c>.
/// </summary>
public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>
    /// Okno wstecz, w którym szukamy „ostatniego podlania” (skoku wilgotności) na potrzeby
    /// wypowiedzi głosowych. Dłuższe = częściej trafimy ostatni epizod, ale wolniej (więcej IO).
    /// </summary>
    [Range(1, 365)]
    public int WateringLookbackDays { get; init; } = 30;

    /// <summary>
    /// Maksymalny lookback (w minutach) dla obliczania „ile minut z rzędu utrzymuje się stan za mokro”.
    /// Po tym oknie zwracamy <c>maxLookbackMinutes + 1</c> i traktujemy jak „od dawna”.
    /// </summary>
    [Range(30, 1440)]
    public int PostWateringWetMaxLookbackMinutes { get; init; } = 240;

    /// <summary>
    /// Próg minut „za mokro z rzędu” poniżej którego status zmienia się z Warning na PostWatering
    /// (krótkotrwałe przemoczenie po podlaniu).
    /// </summary>
    [Range(1, 240)]
    public int PostWateringWetMinutesThreshold { get; init; } = 30;

    public TimeSpan WateringLookback => TimeSpan.FromDays(WateringLookbackDays);
}
