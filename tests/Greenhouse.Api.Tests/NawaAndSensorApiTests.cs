using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Application.Nawy;

namespace Greenhouse.Api.Tests;

public sealed class NawaAndSensorApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public NawaAndSensorApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NawaCrud_ShouldWorkWithNewFields()
    {
        var client = _factory.CreateClient();

        var createResp = await client.PostAsJsonAsync("/api/nawa", new { name = "Nawa test", description = "opis" });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Null(created!.PlantNote);

        var putResp = await client.PutAsJsonAsync(
            $"/api/nawa/{created.Id}",
            new
            {
                name = "Nawa test 2",
                description = (string?)null,
                plantNote = "Pomidory",
                isActive = true,
                moistureMin = 20m,
                moistureMax = 80m,
                temperatureMin = (decimal?)null,
                temperatureMax = (decimal?)null
            });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var updated = await putResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);
        Assert.Equal("Pomidory", updated!.PlantNote);
        Assert.Equal(20m, updated.MoistureMin);
        Assert.Equal(80m, updated.MoistureMax);

        var getResp = await client.GetAsync($"/api/nawa/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);
        Assert.Equal("Pomidory", fetched!.PlantNote);
    }

    [Fact]
    public async Task UpdateNawa_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var resp = await client.PutAsJsonAsync(
            $"/api/nawa/{Guid.NewGuid()}",
            new { name = "X", description = (string?)null, plantNote = (string?)null, isActive = true });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task AssignSensor_UnknownSensor_Returns404()
    {
        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new { name = "SensorTest" });
        var created = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);

        var assignResp = await client.PutAsJsonAsync(
            $"/api/sensor/{Guid.NewGuid()}/nawa",
            new { nawaId = created!.Id });
        Assert.Equal(HttpStatusCode.NotFound, assignResp.StatusCode);
    }
}
