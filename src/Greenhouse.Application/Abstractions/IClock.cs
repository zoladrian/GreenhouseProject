namespace Greenhouse.Application.Abstractions;

/// <summary>
/// Abstrakcja zegara UTC — umożliwia deterministyczne testy integracyjne i scenariusze czasowe bez mockowania całej warstwy domeny.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
