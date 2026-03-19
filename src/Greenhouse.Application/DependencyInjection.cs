using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Readings;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMqttMessageIngestionService, MqttMessageIngestionService>();
        services.AddScoped<GetLatestReadingsQueryService>();
        return services;
    }
}
