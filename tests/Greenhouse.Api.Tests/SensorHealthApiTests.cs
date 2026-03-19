using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Application.Sensors;

namespace Greenhouse.Api.Tests;

public sealed class SensorHealthApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public SensorHealthApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SensorHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/sensor/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var health = await resp.Content.ReadFromJsonAsync<List<SensorHealthDto>>(JsonOptions);
        Assert.NotNull(health);
    }
}
