using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Greenhouse.Infrastructure.Hosting;

/// <summary>
/// Wspólna rejestracja hosted services dla obu hostów (Api/Worker). Jedna zmiana = jedno miejsce,
/// żaden host nie zostaje w tyle z innym setem usług. Sterowane przez konfigurację
/// (sekcje <c>Mqtt</c>, <c>DataLifecycle</c>) i <see cref="GreenhouseHostMode"/>.
/// </summary>
public static class GreenhouseHostingRegistration
{
    public static IServiceCollection AddGreenhouseHostedServices(
        this IServiceCollection services,
        IConfiguration configuration,
        GreenhouseHostMode mode)
    {
        services.AddOptions<MqttOptions>()
            .Bind(configuration.GetSection(MqttOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DataLifecycleOptions>()
            .Bind(configuration.GetSection(DataLifecycleOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Cleanup duplikatów + pruning są tanie i niezależne od MQTT. W obu hostach mają sens,
        // ale w produkcji zwykle uruchamiamy tylko jeden host (API) — tu zachowujemy elastyczność.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, SensorDuplicateCleanupHostedService>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, DataLifecyclePruningHostedService>());

        var mqtt = configuration.GetSection(MqttOptions.SectionName).Get<MqttOptions>() ?? new MqttOptions();
        var enableMqttHere = mqtt.Enabled && mode switch
        {
            GreenhouseHostMode.Api => mqtt.EnableInApiHost,
            GreenhouseHostMode.Worker => mqtt.EnableInWorkerHost,
            _ => false
        };
        if (enableMqttHere)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, MqttIngestionHostedService>());
        }

        return services;
    }
}
