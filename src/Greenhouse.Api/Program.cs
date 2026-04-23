using Greenhouse.Api;
using Greenhouse.Api.Contracts;
using Greenhouse.Api.Mqtt;
using Greenhouse.Application;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Readings;
using Greenhouse.Application.Sensors;
using Greenhouse.Application.Voice;
using Greenhouse.Application.Weather;
using Greenhouse.Infrastructure;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonUtcDateTimeConverter());
    options.SerializerOptions.Converters.Add(new JsonUtcNullableDateTimeConverter());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SensorDuplicateCleanupHostedService>();
builder.Services.Configure<VoiceOptions>(builder.Configuration.GetSection(VoiceOptions.SectionName));
builder.Services.Configure<WeatherInterpretationOptions>(builder.Configuration.GetSection(WeatherInterpretationOptions.SectionName));

var mqttOpts = new MqttOptions();
builder.Configuration.GetSection(MqttOptions.SectionName).Bind(mqttOpts);
if (mqttOpts.Enabled)
{
    builder.Services.AddHostedService<MqttIngestionHostedService>();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await SqlitePragmas.ApplyWalAsync(dbContext);
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/meta/deploy", () => Results.Json(new { deployId = DeployInfo.DeployId }));

// ─── Readings ──────────────────────────────────────────────
app.MapGet("/api/readings/latest", async (int? count, GetLatestReadingsQueryService query, CancellationToken ct) =>
{
    var result = await query.ExecuteAsync(count ?? 50, ct);
    return Results.Ok(result);
});

app.MapGet("/api/readings/history/{sensorId:guid}", async (
    Guid sensorId, DateTime? from, DateTime? to,
    GetReadingHistoryQueryService query, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-24);
    var toUtc = to ?? DateTime.UtcNow;
    var result = await query.ExecuteAsync(sensorId, fromUtc, toUtc, ct);
    return Results.Ok(result);
});

// ─── Voice (offline daily report) ──────────────────────────
app.MapGet("/api/voice/daily-report", async (GetVoiceDailyReportQueryService query, CancellationToken ct) =>
{
    var report = await query.ExecuteAsync(ct);
    return Results.Ok(report);
});

app.MapGet("/api/voice/nawa/{id:guid}/brief", async (Guid id, GetNawaVoiceBriefQueryService query, CancellationToken ct) =>
{
    var brief = await query.ExecuteAsync(id, ct);
    return brief is null ? Results.NotFound() : Results.Ok(brief);
});

// ─── Dashboard ─────────────────────────────────────────────
app.MapGet("/api/dashboard", async (GetDashboardQueryService query, CancellationToken ct) =>
{
    var snapshots = await query.ExecuteAsync(ct);
    return Results.Ok(snapshots);
});

// ─── Nawy CRUD ─────────────────────────────────────────────
app.MapPost("/api/nawa", async (CreateNawaRequest body, CreateNawaCommandService command, CancellationToken ct) =>
{
    try
    {
        var dto = await command.ExecuteAsync(body.Name, body.Description, ct);
        return Results.Created($"/api/nawa/{dto.Id}", dto);
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/nawa", async (ListNawyQueryService query, CancellationToken ct) =>
{
    var list = await query.ExecuteAsync(ct);
    return Results.Ok(list);
});

app.MapGet("/api/nawa/{id:guid}", async (Guid id, GetNawaQueryService query, CancellationToken ct) =>
{
    var dto = await query.ExecuteAsync(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.MapGet("/api/nawa/{id:guid}/detail", async (Guid id, GetNawaDetailQueryService query, CancellationToken ct) =>
{
    var dto = await query.ExecuteAsync(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.MapPut("/api/nawa/{id:guid}", async (Guid id, UpdateNawaRequest body, UpdateNawaCommandService command, CancellationToken ct) =>
{
    try
    {
        var dto = await command.ExecuteAsync(
            id, body.Name, body.Description, body.PlantNote, body.IsActive,
            body.MoistureMin, body.MoistureMax, body.TemperatureMin, body.TemperatureMax, ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ─── Sensors ───────────────────────────────────────────────
app.MapGet("/api/sensor", async (ListSensorsQueryService query, CancellationToken ct) =>
{
    var list = await query.ExecuteAsync(ct);
    return Results.Ok(list);
});

app.MapGet("/api/sensor/{sensorId:guid}", async (Guid sensorId, GetSensorDetailQueryService query, CancellationToken ct) =>
{
    var dto = await query.ExecuteAsync(sensorId, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.MapPut("/api/sensor/{sensorId:guid}/nawa", async (Guid sensorId, AssignSensorNawaRequest body, AssignSensorToNawaCommandService command, CancellationToken ct) =>
{
    var result = await command.ExecuteAsync(sensorId, body.NawaId, ct);
    return result switch
    {
        AssignSensorResult.Ok => Results.NoContent(),
        AssignSensorResult.SensorNotFound => Results.NotFound(new { error = "Nie znaleziono czujnika." }),
        AssignSensorResult.NawaNotFound => Results.NotFound(new { error = "Nie znaleziono nawy." }),
        _ => Results.Problem()
    };
});

app.MapPut("/api/sensor/{sensorId:guid}/display-name", async (Guid sensorId, UpdateSensorDisplayNameRequest body, UpdateSensorDisplayNameCommandService command, CancellationToken ct) =>
{
    var dto = await command.ExecuteAsync(sensorId, body.DisplayName, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.MapDelete("/api/sensor/{sensorId:guid}", async (Guid sensorId, DeleteSensorCommandService command, CancellationToken ct) =>
{
    var ok = await command.ExecuteAsync(sensorId, ct);
    return ok ? Results.NoContent() : Results.NotFound(new { error = "Nie znaleziono czujnika." });
});

app.MapGet("/api/sensor/health", async (GetSensorHealthQueryService query, CancellationToken ct) =>
{
    var list = await query.ExecuteAsync(ct);
    return Results.Ok(list);
});

// ─── Charts / Analytics ────────────────────────────────────
app.MapGet("/api/chart/moisture", async (
    Guid? nawaId, Guid? sensorId, DateTime? from, DateTime? to,
    GetMoistureSeriesQueryService query, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-24);
    var toUtc = to ?? DateTime.UtcNow;
    var data = await query.ExecuteAsync(nawaId, sensorId, fromUtc, toUtc, ct);
    return Results.Ok(data);
});

app.MapGet("/api/chart/weather", async (
    Guid? nawaId, Guid? sensorId, DateTime? from, DateTime? to,
    GetWeatherSeriesQueryService query, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-24);
    var toUtc = to ?? DateTime.UtcNow;
    var data = await query.ExecuteAsync(nawaId, sensorId, fromUtc, toUtc, ct);
    return Results.Ok(data);
});

app.MapGet("/api/chart/watering-events", async (
    Guid nawaId, DateTime? from, DateTime? to,
    GetWateringEventsQueryService query, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-48);
    var toUtc = to ?? DateTime.UtcNow;
    var events = await query.ExecuteAsync(nawaId, fromUtc, toUtc, ct);
    return Results.Ok(events);
});

app.MapGet("/api/chart/drying-rate", async (
    Guid nawaId, DateTime? from, DateTime? to,
    GetDryingRatesQueryService query, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-24);
    var toUtc = to ?? DateTime.UtcNow;
    var rates = await query.ExecuteAsync(nawaId, fromUtc, toUtc, ct);
    return Results.Ok(rates);
});

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
