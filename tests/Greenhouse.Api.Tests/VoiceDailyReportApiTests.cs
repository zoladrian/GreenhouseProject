using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Greenhouse.Application.Voice;

namespace Greenhouse.Api.Tests;

public sealed class VoiceDailyReportApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly GreenhouseWebApplicationFactory _factory;

    public VoiceDailyReportApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task VoiceDailyReport_ShouldReturnOk_AndShape()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/nawa", new { name = "Raport nawa" });

        var resp = await client.GetAsync("/api/voice/daily-report");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<VoiceDailyReportDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.GreetingLeadin));
        Assert.False(string.IsNullOrWhiteSpace(dto.LocalTime));
        Assert.False(string.IsNullOrWhiteSpace(dto.LocalDateLong));
        Assert.NotEmpty(dto.Nawy);
        var line = Assert.Single(dto.Nawy, n => n.NawaName == "Raport nawa");
        Assert.Equal(1, line.Order);
        Assert.Equal(0, line.AssignedSensorCount);
        Assert.False(string.IsNullOrWhiteSpace(line.MoistureAssessment));
        Assert.False(string.IsNullOrWhiteSpace(line.TemperatureAssessment));
    }
}
