using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Greenhouse.Api.Tests;

/// <summary>
/// CORS jest krytyczny dla rozdzielonego front (PWA pod inną domeną/portem niż API). 
/// Brak preflight = zablokowane mutacje z przeglądarki.
/// </summary>
public sealed class CorsPreflightApiTests : IClassFixture<CorsPreflightApiTests.CorsAllowedFactory>
{
    private const string AllowedOrigin = "https://kwiaty.example.com";

    private readonly CorsAllowedFactory _factory;

    public CorsPreflightApiTests(CorsAllowedFactory factory) => _factory = factory;

    [Fact]
    public async Task Options_Preflight_ShouldReturnNoContent_AndAllowMethodsHeader_ForAllowedOrigin()
    {
        var client = _factory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Options, "/api/nawa");
        req.Headers.Add("Origin", AllowedOrigin);
        req.Headers.Add("Access-Control-Request-Method", "POST");
        req.Headers.Add("Access-Control-Request-Headers", "content-type, x-api-key");

        var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.True(resp.Headers.Contains("Access-Control-Allow-Origin"),
            "Brak nagłówka Access-Control-Allow-Origin dla dozwolonego origin.");
        var allowOrigin = string.Join(",", resp.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.Equal(AllowedOrigin, allowOrigin);
        Assert.True(resp.Headers.Contains("Access-Control-Allow-Methods"));
    }

    [Fact]
    public async Task Options_Preflight_ShouldNotEcho_DisallowedOrigin()
    {
        var client = _factory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Options, "/api/nawa");
        req.Headers.Add("Origin", "https://evil.example.com");
        req.Headers.Add("Access-Control-Request-Method", "POST");

        var resp = await client.SendAsync(req);

        Assert.False(resp.Headers.Contains("Access-Control-Allow-Origin"),
            "Niedozwolony origin nie może dostać Access-Control-Allow-Origin.");
    }

    [Fact]
    public async Task GetRequest_ShouldEcho_AllowedOriginHeader()
    {
        var client = _factory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/nawa");
        req.Headers.Add("Origin", AllowedOrigin);

        var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(resp.Headers.Contains("Access-Control-Allow-Origin"));
    }

    public sealed class CorsAllowedFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-cors-{Guid.NewGuid()}.db");
            builder.UseSetting("Infrastructure:DatabasePath", dbPath);
            builder.UseSetting("Mqtt:Enabled", "false");
            builder.UseSetting("ApiSecurity:RequireForMutations", "false");
            builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        }
    }
}
