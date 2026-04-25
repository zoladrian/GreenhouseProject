using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Time;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Readings;
using Greenhouse.Application.Sensors;
using Greenhouse.Application.Voice;
using Greenhouse.Application.Weather;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Greenhouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IGreenhouseTimeZoneResolver, GreenhouseTimeZoneResolver>();
        services.AddSingleton<IMqttIngestTelemetry, MqttIngestTelemetry>();
        services.AddScoped<IMqttMessageIngestionService, MqttMessageIngestionService>();
        services.AddScoped<ISensorProvisioningService, SensorProvisioningService>();

        services.AddScoped<GetLatestReadingsQueryService>();
        services.AddScoped<GetReadingHistoryQueryService>();

        services.AddScoped<CreateNawaCommandService>();
        services.AddScoped<UpdateNawaCommandService>();
        services.AddScoped<ListNawyQueryService>();
        services.AddScoped<GetNawaQueryService>();
        services.AddScoped<GetNawaDetailQueryService>();
        services.AddScoped<GetDashboardQueryService>();

        services.AddScoped<ListSensorsQueryService>();
        services.AddScoped<DeleteSensorCommandService>();
        services.AddScoped<AssignSensorToNawaCommandService>();
        services.AddScoped<GetSensorHealthQueryService>();
        services.AddScoped<GetSensorDetailQueryService>();
        services.AddScoped<UpdateSensorDisplayNameCommandService>();

        services.AddScoped<GetMoistureSeriesQueryService>();
        services.AddScoped<GetWeatherSeriesQueryService>();
        services.AddScoped<GetWateringEventsQueryService>();
        services.AddScoped<GetDryingRatesQueryService>();
        services.AddScoped<WeatherInterpretationService>();
        services.AddScoped<WeatherControlConfigService>();

        services.AddScoped<GetVoiceDailyReportQueryService>();
        services.AddScoped<GetVoiceWeatherReportQueryService>();
        services.AddScoped<GetNawaVoiceBriefQueryService>();
        services.AddSingleton<IValidateOptions<WeatherInterpretationOptions>, WeatherInterpretationOptionsValidator>();

        return services;
    }
}
