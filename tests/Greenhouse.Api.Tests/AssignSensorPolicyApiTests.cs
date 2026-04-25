using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Api.Contracts;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Sensors;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Api.Tests;

public sealed class AssignSensorPolicyApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public AssignSensorPolicyApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PutAssign_ShouldReject_AssigningGlobalWeatherSensorToNawa()
    {
        var name = $"rain_policy_{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
            await ingest.IngestAsync(
                new IncomingMqttMessage(
                    $"zigbee2mqtt/{name}",
                    "{\"rain\":true,\"illuminance_raw\":100,\"battery\":70}",
                    now),
                CancellationToken.None);
        }

        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new CreateNawaRequest($"NawaPolicy_{name}", null));
        var nawa = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);
        Assert.NotNull(nawa);

        var sensors = await client.GetFromJsonAsync<List<SensorListItemDto>>("/api/sensor", JsonOptions);
        var weather = Assert.Single(sensors!, s => s.ExternalId == name || s.DisplayName == name);
        Assert.Equal("Weather", weather.Kind);

        var resp = await client.PutAsJsonAsync(
            $"/api/sensor/{weather.Id}/nawa",
            new AssignSensorNawaRequest(nawa!.Id));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PutAssign_ShouldAllow_UnassignViaNull()
    {
        var name = $"rain_unassign_{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
            await ingest.IngestAsync(
                new IncomingMqttMessage(
                    $"zigbee2mqtt/{name}",
                    "{\"rain\":false,\"illuminance_raw\":50,\"battery\":88}",
                    now),
                CancellationToken.None);
        }

        var client = _factory.CreateClient();
        var sensors = await client.GetFromJsonAsync<List<SensorListItemDto>>("/api/sensor", JsonOptions);
        var weather = Assert.Single(sensors!, s => s.ExternalId == name || s.DisplayName == name);

        var resp = await client.PutAsJsonAsync(
            $"/api/sensor/{weather.Id}/nawa",
            new AssignSensorNawaRequest(null));

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }
}
