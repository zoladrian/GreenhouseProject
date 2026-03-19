using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Application.Nawy;

namespace Greenhouse.Api.Tests;

public sealed class DashboardApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public DashboardApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ShouldShowCreatedNawa()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/nawa", new { name = "Dashboard nawa" });

        var resp = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var snapshots = await resp.Content.ReadFromJsonAsync<List<NawaSnapshotDto>>(JsonOptions);
        Assert.NotNull(snapshots);
        Assert.Contains(snapshots!, s => s.NawaName == "Dashboard nawa");
    }
}
