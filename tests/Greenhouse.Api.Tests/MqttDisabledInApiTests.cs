using Greenhouse.Api.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Greenhouse.Api.Tests;

public sealed class MqttDisabledInApiTests : IClassFixture<GreenhouseWebApplicationFactory>
{
    private readonly GreenhouseWebApplicationFactory _factory;

    public MqttDisabledInApiTests(GreenhouseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void HostedServices_ShouldNotIncludeMqttIngestion_WhenMqttDisabled()
    {
        _factory.CreateClient();

        var hosted = _factory.Services.GetServices<IHostedService>();
        Assert.DoesNotContain(hosted, s => s is MqttIngestionHostedService);
    }
}
