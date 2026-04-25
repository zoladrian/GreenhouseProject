using Microsoft.Extensions.Logging;

namespace Greenhouse.Application.Time;

public sealed class GreenhouseTimeZoneResolver : IGreenhouseTimeZoneResolver
{
    private readonly ILogger<GreenhouseTimeZoneResolver> _logger;

    public GreenhouseTimeZoneResolver(ILogger<GreenhouseTimeZoneResolver> logger)
    {
        _logger = logger;
    }

    public TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
        {
            _logger.LogWarning(
                ex,
                "Nieznana strefa czasowa '{TimeZoneId}' — degraduję do UTC. Sprawdź ustawienie Voice:TimeZoneId.",
                timeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
