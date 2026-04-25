using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Api.Contracts;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Sensors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Greenhouse.Api.Tests;

/// <summary>
/// Regresja: stary <c>SampleMax</c> tnący spłaszczoną listę gubił czujniki przy multi-sensor scope.
/// Po przejściu na <see cref="SeriesDecimator"/> każda seria ma swój własny budżet punktów.
/// </summary>
public sealed class MultiSensorDecimationApiTests : IClassFixture<MultiSensorDecimationApiTests.LowLimitFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly LowLimitFactory _factory;

    public MultiSensorDecimationApiTests(LowLimitFactory factory) => _factory = factory;

    [Fact]
    public async Task MoistureSeries_KeepsBothSensors_WhenMaxPointsPerSeriesIsLow()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var soilA = $"soilA_{suffix}";
        var soilB = $"soilB_{suffix}";
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IMqttMessageIngestionService>();
            // ~30 punktów per czujnik — wyraźnie powyżej testowego limitu MaxPointsPerSeries=10.
            for (var i = 0; i < 30; i++)
            {
                await ingest.IngestAsync(
                    new IncomingMqttMessage(
                        $"zigbee2mqtt/{soilA}",
                        $"{{\"soil_moisture\":{30 + i},\"temperature\":21,\"battery\":91,\"linkquality\":150}}",
                        now.AddMinutes(-30 + i)),
                    CancellationToken.None);
                await ingest.IngestAsync(
                    new IncomingMqttMessage(
                        $"zigbee2mqtt/{soilB}",
                        $"{{\"soil_moisture\":{60 + i},\"temperature\":22,\"battery\":80,\"linkquality\":160}}",
                        now.AddMinutes(-30 + i)),
                    CancellationToken.None);
            }
        }

        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/api/nawa", new CreateNawaRequest($"NawaMulti_{suffix}", null));
        var nawa = await createResp.Content.ReadFromJsonAsync<NawaDto>(JsonOptions);
        var sensors = await client.GetFromJsonAsync<List<SensorListItemDto>>("/api/sensor", JsonOptions);
        var a = Assert.Single(sensors!, s => s.ExternalId == soilA);
        var b = Assert.Single(sensors!, s => s.ExternalId == soilB);
        await client.PutAsJsonAsync($"/api/sensor/{a.Id}/nawa", new AssignSensorNawaRequest(nawa!.Id));
        await client.PutAsJsonAsync($"/api/sensor/{b.Id}/nawa", new AssignSensorNawaRequest(nawa.Id));

        var from = Uri.EscapeDataString(now.AddMinutes(-60).ToString("O"));
        var to = Uri.EscapeDataString(now.AddMinutes(1).ToString("O"));
        var resp = await client.GetAsync($"/api/chart/moisture?nawaId={nawa.Id}&from={from}&to={to}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var rows = await resp.Content.ReadFromJsonAsync<List<MoistureSeriesPointDto>>(JsonOptions);
        Assert.NotNull(rows);

        var perSensor = rows!.Where(r => r.SensorId.HasValue).GroupBy(r => r.SensorId!.Value).ToDictionary(g => g.Key, g => g.Count());

        // Decymacja per-series: każdy czujnik dostaje do 10 punktów (limit testowy),
        // ŻADEN nie zostaje wyzerowany — to była główna regresja zgłoszona w P0.
        Assert.True(perSensor.ContainsKey(a.Id), "Czujnik A musi pozostać po decymacji.");
        Assert.True(perSensor.ContainsKey(b.Id), "Czujnik B musi pozostać po decymacji.");
        Assert.InRange(perSensor[a.Id], 1, 10);
        Assert.InRange(perSensor[b.Id], 1, 10);

        // Sanity: kolejność czasowa odpowiedzi globalnie zachowana.
        for (var i = 1; i < rows!.Count; i++)
            Assert.True(rows[i - 1].UtcTime <= rows[i].UtcTime, $"Punkt {i} narusza monotoniczność czasu.");
    }

    public sealed class LowLimitFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-multi-decim-{Guid.NewGuid()}.db");
            builder.UseSetting("Infrastructure:DatabasePath", dbPath);
            builder.UseSetting("Mqtt:Enabled", "false");
            builder.UseSetting("ApiSecurity:RequireForMutations", "false");
            builder.UseSetting("Charts:MaxPointsPerSeries", "10");
        }
    }
}
