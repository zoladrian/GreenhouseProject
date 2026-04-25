using System.Net;

namespace Greenhouse.Api.Tests;

/// <summary>
/// Regresja: nieznane /api/* nie może być przykryte SPA fallbackiem (HTML 200).
/// Frontend musi dostać JSON 404, żeby fetchJson mógł go normalnie sparsować.
/// </summary>
public sealed class ApiNotFoundFallbackTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public ApiNotFoundFallbackTests(GreenhouseWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UnknownApiPath_Returns404Json_NotSpaHtml()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/this-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var contentType = resp.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownApiPathWithNestedSegments_Returns404Json()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/foo/bar/baz");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("application/json", resp.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task RootSpaPath_StillReturnsHtml()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/");

        // SPA shell musi działać dalej dla aplikacji webowej.
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
