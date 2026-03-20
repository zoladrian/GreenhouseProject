namespace Greenhouse.Api.Tests;

public sealed class DeployMetaApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public DeployMetaApiTests(GreenhouseWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDeployMeta_ShouldReturnJson_WithDeployId()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/meta/deploy");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("deployId", json, StringComparison.OrdinalIgnoreCase);
    }
}
