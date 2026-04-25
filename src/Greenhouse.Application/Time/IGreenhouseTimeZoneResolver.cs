namespace Greenhouse.Application.Time;

/// <summary>
/// Centralna rozdzielnia stref czasowych — eliminuje 3 niezależne kopie metody <c>ResolveTimeZone</c>
/// (dashboard, voice brief, voice daily) z cichym <c>catch</c>. Niewłaściwy <c>timeZoneId</c>
/// loguje ostrzeżenie i degraduje do <see cref="TimeZoneInfo.Utc"/>, zamiast przemilczeć błąd.
/// </summary>
public interface IGreenhouseTimeZoneResolver
{
    TimeZoneInfo Resolve(string? timeZoneId);
}
