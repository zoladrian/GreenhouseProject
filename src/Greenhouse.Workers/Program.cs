using Greenhouse.Workers;
using Greenhouse.Application;
using Greenhouse.Infrastructure;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await SqlitePragmas.ApplyWalAsync(dbContext);
}

host.Run();
