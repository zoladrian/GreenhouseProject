using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Time;

/// <summary>Produkcyjna implementacja <see cref="IClock"/> oparta o zegar systemowy.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
