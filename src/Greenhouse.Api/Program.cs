using Greenhouse.Application;
using Greenhouse.Application.Readings;
using Greenhouse.Infrastructure;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapGet("/api/readings/latest", async (
    int? count,
    GetLatestReadingsQueryService query,
    CancellationToken cancellationToken) =>
{
    var result = await query.ExecuteAsync(count ?? 50, cancellationToken);
    return Results.Ok(result);
});

app.Run();

public partial class Program;
