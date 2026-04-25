using Greenhouse.Domain.Weather;

namespace Greenhouse.Application.Abstractions;

public interface IWeatherControlConfigRepository
{
    Task<WeatherControlConfig> GetOrCreateAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
