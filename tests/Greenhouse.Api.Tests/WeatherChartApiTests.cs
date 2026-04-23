using System.Net;
using System.Net.Http.Json;
using Greenhouse.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Api.Tests;

public sealed class WeatherChartApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public WeatherChartApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WeatherChartEndpoint_ShouldReturnWeatherSeries_WithTimeFilter()
    {
        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
            await ingest.IngestAsync(
                new IncomingMqttMessage(
                    "zigbee2mqtt/Deszcz_Chart",
                    "{\"rain\":true,\"rain_intensity\":16,\"illuminance_raw\":480,\"illuminance_average_20min\":450,\"illuminance_maximum_today\":900,\"cleaning_reminder\":false}",
                    now.AddMinutes(-20)),
                CancellationToken.None);
            await ingest.IngestAsync(
                new IncomingMqttMessage(
                    "zigbee2mqtt/Deszcz_Chart",
                    "{\"rain\":false,\"rain_intensity\":2,\"illuminance_raw\":700,\"battery\":84}",
                    now.AddMinutes(-2)),
                CancellationToken.None);
        }

        var client = _factory.CreateClient();
        var from = Uri.EscapeDataString(now.AddMinutes(-30).ToString("O"));
        var to = Uri.EscapeDataString(now.ToString("O"));
        var sensors = await client.GetFromJsonAsync<List<SensorListItemResponse>>("/api/sensor");
        var sensorId = Assert.Single(sensors!).Id;

        var response = await client.GetAsync($"/api/chart/weather?sensorId={sensorId}&from={from}&to={to}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = await response.Content.ReadFromJsonAsync<List<WeatherPointResponse>>();
        Assert.NotNull(rows);
        Assert.NotEmpty(rows!);
        Assert.Contains(rows, x => x.RainIntensityRaw == 16m && x.Rain == true);
        Assert.All(rows, x => Assert.True(x.UtcTime >= now.AddMinutes(-30) && x.UtcTime <= now));
    }

    private sealed record WeatherPointResponse(
        DateTime UtcTime,
        bool? Rain,
        decimal? RainIntensityRaw,
        decimal? IlluminanceRaw);

    private sealed record SensorListItemResponse(Guid Id);
}
