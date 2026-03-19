using System.Net;

namespace Greenhouse.Api.Tests;

public sealed class LatestReadingsEndpointTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public LatestReadingsEndpointTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLatest_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/readings/latest?count=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
