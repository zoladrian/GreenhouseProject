using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Api.Contracts;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Sensors;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Api.Tests;

/// <summary>
/// Regresja zakresu czujników na wykresach nawy: gleba z nawy + globalna pogoda (bez duplikowania per nawa).
/// </summary>
public sealed class NawaScopedChartsApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public NawaScopedChartsApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MoistureSeries_ForNawa_IncludesReadingsFromGlobalWeatherSensor()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var soilName = $"soil_scope_{suffix}";
        var rainName = $"rain_scope_{suffix}";
        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
            await ingest.IngestAsync(
                new IncomingMqttMessage(
                    $"zigbee2mqtt/{soilName}",
                    "{\"soil_moisture\":41,\"temperature\":20.5,\"battery\":92,\"linkquality\":200}",
                    now.AddMinutes(-15)),
                CancellationToken.None);
            await ingest.IngestAsync(
                new IncomingMqttMessage(
                    $"zigbee2mqtt/{rainName}",
                    "{\"rain\":false,\"illuminance_raw\":410,\"battery\":81,\"linkquality\":180}",
                    now.AddMinutes(-4)),
                CancellationToken.None);
        }

        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new CreateNawaRequest($"NawaScope_{suffix}", null));
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var nawa = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);
        Assert.NotNull(nawa);

        var sensors = await client.GetFromJsonAsync<List<SensorListItemDto>>("/api/sensor", JsonOptions);
        Assert.NotNull(sensors);
        var soil = Assert.Single(sensors!, s => s.ExternalId == soilName || s.DisplayName == soilName);
        var rain = Assert.Single(sensors!, s => s.ExternalId == rainName || s.DisplayName == rainName);
        Assert.Equal("Weather", rain.Kind);
        Assert.Null(rain.NawaId);

        var assignResp = await client.PutAsJsonAsync(
            $"/api/sensor/{soil.Id}/nawa",
            new AssignSensorNawaRequest(nawa!.Id));
        Assert.Equal(HttpStatusCode.NoContent, assignResp.StatusCode);

        var from = Uri.EscapeDataString(now.AddMinutes(-30).ToString("O"));
        var to = Uri.EscapeDataString(now.AddMinutes(1).ToString("O"));
        var moistureResp = await client.GetAsync($"/api/chart/moisture?nawaId={nawa.Id}&from={from}&to={to}");
        Assert.Equal(HttpStatusCode.OK, moistureResp.StatusCode);
        var moistureRows = await moistureResp.Content.ReadFromJsonAsync<List<MoistureSeriesPointDto>>(JsonOptions);
        Assert.NotNull(moistureRows);
        var distinctSensorIds = moistureRows!.Where(r => r.SensorId.HasValue).Select(r => r.SensorId!.Value).Distinct().ToHashSet();
        Assert.Contains(soil.Id, distinctSensorIds);
        Assert.Contains(rain.Id, distinctSensorIds);

        var weatherResp = await client.GetAsync($"/api/chart/weather?nawaId={nawa.Id}&from={from}&to={to}");
        Assert.Equal(HttpStatusCode.OK, weatherResp.StatusCode);
        var weatherRows = await weatherResp.Content.ReadFromJsonAsync<List<WeatherSeriesPointDto>>(JsonOptions);
        Assert.NotNull(weatherRows);
        Assert.NotEmpty(weatherRows!);
        Assert.All(weatherRows, r => Assert.Equal(rain.Id, r.SensorId));
    }
}
