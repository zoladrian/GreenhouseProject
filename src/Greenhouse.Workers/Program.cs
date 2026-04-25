using Greenhouse.Application;
using Greenhouse.Infrastructure;
using Greenhouse.Infrastructure.Hosting;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// Te same hosted services co API — flagi w konfiguracji decydują, kto naprawdę słucha brokera.
builder.Services.AddGreenhouseHostedServices(builder.Configuration, GreenhouseHostMode.Worker);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
    await dbContext.Database.MigrateAsync();
    await SqlitePragmas.ApplyWalAsync(dbContext);
}

host.Run();
