using Greenhouse.Application.Abstractions;
using Greenhouse.Infrastructure.Mqtt;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(InfrastructureOptions.SectionName).Get<InfrastructureOptions>() ?? new InfrastructureOptions();

        var databasePath = Path.GetFullPath(options.DatabasePath);
        var databaseDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        services.AddSingleton<SqlitePragmaConnectionInterceptor>();

        services.AddDbContext<GreenhouseDbContext>((sp, db) =>
        {
            db.UseSqlite(SqliteConnectionFactory.BuildConnectionString(databasePath));
            db.AddInterceptors(sp.GetRequiredService<SqlitePragmaConnectionInterceptor>());
        });

        services.AddScoped<ISensorReadingRepository, EfSensorReadingRepository>();
        services.AddScoped<INawaRepository, EfNawaRepository>();
        services.AddScoped<ISensorRepository, EfSensorRepository>();
        services.AddScoped<IWeatherControlConfigRepository, EfWeatherControlConfigRepository>();
        services.AddScoped<ISunScheduleRepository, EfSunScheduleRepository>();
        services.AddScoped<ISensorDuplicateMerger, EfSensorDuplicateMerger>();
        services.AddSingleton<IMqttPayloadParser, JsonMqttPayloadParser>();

        return services;
    }
}
