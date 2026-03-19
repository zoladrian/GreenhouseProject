using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Sensors;

namespace Greenhouse.Api.Tests;

public sealed class DetailApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public DetailApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NawaDetail_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new { name = "Detail nawa" });
        var nawa = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);

        var resp = await client.GetAsync($"/api/nawa/{nawa!.Id}/detail");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var detail = await resp.Content.ReadFromJsonAsync<NawaDetailDto>(JsonOptions);
        Assert.Equal("Detail nawa", detail!.Name);
        Assert.Empty(detail.Sensors);
    }

    [Fact]
    public async Task NawaDetail_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/nawa/{Guid.NewGuid()}/detail");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task SensorDetail_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/sensor/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task ReadingHistory_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var sensorId = Guid.NewGuid();
        var resp = await client.GetAsync($"/api/readings/history/{sensorId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSensorDisplayName_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync(
            $"/api/sensor/{Guid.NewGuid()}/display-name",
            new { displayName = "Test" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
