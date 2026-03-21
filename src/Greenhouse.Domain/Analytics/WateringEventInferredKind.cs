namespace Greenhouse.Domain.Analytics;

/// <summary>
/// Heurystyka źródła skoku wilgotności (bez danych pogodowych).
/// <see cref="LikelyRain"/> gdy w tym samym epizodzie czasowym skok widzi ≥2 czujniki w nawie (równomierne zmoczenie).
/// </summary>
public enum WateringEventInferredKind
{
    Unknown = 0,
    LikelyManual = 1,
    LikelyRain = 2
}
