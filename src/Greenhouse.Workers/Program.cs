using Greenhouse.Workers;
using Greenhouse.Application;
using Greenhouse.Infrastructure;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection(MqttOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var mqtt = builder.Configuration.GetSection(MqttOptions.SectionName).Get<MqttOptions>() ?? new MqttOptions();
if (mqtt.Enabled && mqtt.EnableInWorkerHost)
{
    builder.Services.AddHostedService<Worker>();
}

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
    await dbContext.Database.MigrateAsync();
    await SqlitePragmas.ApplyWalAsync(dbContext);
}

host.Run();
