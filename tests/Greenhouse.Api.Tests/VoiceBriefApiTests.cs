using System.Net;
using System.Net.Http.Json;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Voice;

namespace Greenhouse.Api.Tests;

public sealed class VoiceBriefApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public VoiceBriefApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task VoiceBrief_ShouldReturnNotFound_ForUnknownNawa()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/voice/nawa/{Guid.NewGuid()}/brief");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task VoiceBrief_ShouldReturnOk_ForExistingNawa()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/nawa", new { name = "Brief nawa" });
        var nawa = await create.Content.ReadFromJsonAsync<NawaDto>();

        var resp = await client.GetAsync($"/api/voice/nawa/{nawa!.Id}/brief");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<NawaVoiceBriefDto>();
        Assert.NotNull(dto);
        Assert.Equal("Brief nawa", dto!.NawaName);
        Assert.False(string.IsNullOrWhiteSpace(dto.SpokenText));
    }
}
