using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Greenhouse.Api.Tests;

public sealed class LatestReadingsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LatestReadingsEndpointTests(WebApplicationFactory<Program> factory)
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
