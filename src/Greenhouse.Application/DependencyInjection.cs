using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Ingestion;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Readings;
using Greenhouse.Application.Sensors;
using Greenhouse.Application.Voice;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
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
        services.AddScoped<AssignSensorToNawaCommandService>();
        services.AddScoped<GetSensorHealthQueryService>();
        services.AddScoped<GetSensorDetailQueryService>();
        services.AddScoped<UpdateSensorDisplayNameCommandService>();

        services.AddScoped<GetMoistureSeriesQueryService>();
        services.AddScoped<GetWateringEventsQueryService>();
        services.AddScoped<GetDryingRatesQueryService>();

        services.AddScoped<GetVoiceDailyReportQueryService>();

        return services;
    }
}
