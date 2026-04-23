using System.Net;
using System.Net.Http.Json;

namespace Greenhouse.Api.Tests;

public sealed class HealthApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public HealthApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveAndReadyEndpoints_ShouldReturnOk()
    {
        var client = _factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task IngestMetaEndpoint_ShouldReturnCounters()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/meta/ingest");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var payload = await resp.Content.ReadFromJsonAsync<IngestMetaResponse>();
        Assert.NotNull(payload);
    }

    private sealed record IngestMetaResponse(long BrokerMessages, long SkippedTopics, long ReadingsPersisted);
}
