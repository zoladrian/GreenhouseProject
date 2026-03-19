using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Nawy;

namespace Greenhouse.Api.Tests;

public sealed class ChartApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public ChartApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MoistureSeries_WithoutParams_Returns400OrOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/chart/moisture");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await resp.Content.ReadFromJsonAsync<List<MoistureSeriesPointDto>>(JsonOptions);
        Assert.NotNull(data);
        Assert.Empty(data!);
    }

    [Fact]
    public async Task WateringEvents_ForNawa_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new { name = "ChartNawa" });
        var nawa = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);

        var resp = await client.GetAsync($"/api/chart/watering-events?nawaId={nawa!.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var events = await resp.Content.ReadFromJsonAsync<List<WateringEventDto>>(JsonOptions);
        Assert.NotNull(events);
        Assert.Empty(events!);
    }

    [Fact]
    public async Task DryingRate_ForNawa_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new { name = "DryingNawa" });
        var nawa = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);

        var resp = await client.GetAsync($"/api/chart/drying-rate?nawaId={nawa!.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
