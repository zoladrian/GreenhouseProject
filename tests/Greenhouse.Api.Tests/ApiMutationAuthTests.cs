using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace Greenhouse.Api.Tests;

public sealed class ApiMutationAuthTests
{
    [Fact]
    public async Task MutationEndpoint_ShouldReturnUnauthorized_WhenApiKeyMissing()
    {
        await using var factory = new AuthRequiredFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/nawa", new { name = "Auth test" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task MutationEndpoint_ShouldAllow_WhenApiKeyProvided()
    {
        await using var factory = new AuthRequiredFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key-123");

        var resp = await client.PostAsJsonAsync("/api/nawa", new { name = "Auth test" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    private sealed class AuthRequiredFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-auth-test-{Guid.NewGuid()}.db");
            builder.UseSetting("Infrastructure:DatabasePath", dbPath);
            builder.UseSetting("Mqtt:Enabled", "false");
            builder.UseSetting("ApiSecurity:RequireForMutations", "true");
            builder.UseSetting("ApiSecurity:ApiKey", "test-api-key-123");
        }
    }
}
