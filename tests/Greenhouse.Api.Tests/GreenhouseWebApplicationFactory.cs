using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Greenhouse.Api.Tests;

/// <summary>
/// Izolowana baza SQLite na czas testów (unika konfliktu ze starym schematem na dysku).
/// </summary>
public sealed class GreenhouseWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-api-test-{Guid.NewGuid()}.db");
        builder.UseSetting("Infrastructure:DatabasePath", dbPath);
        builder.UseSetting("Mqtt:Enabled", "false");
        builder.UseSetting("ApiSecurity:RequireForMutations", "false");
    }
}
