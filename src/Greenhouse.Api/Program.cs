using Greenhouse.Api;
using Greenhouse.Api.Contracts;
using Greenhouse.Api.Charts;
using Greenhouse.Api.Cors;
using Greenhouse.Api.Mqtt;
using Greenhouse.Api.Operations;
using Greenhouse.Api.Security;
using Greenhouse.Application;
using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Charts;
using Greenhouse.Application.Nawy;
using Greenhouse.Application.Readings;
using Greenhouse.Application.Sensors;
using Greenhouse.Application.Voice;
using Greenhouse.Application.Weather;
using Greenhouse.Infrastructure;
using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonUtcDateTimeConverter());
    options.SerializerOptions.Converters.Add(new JsonUtcNullableDateTimeConverter());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SensorDuplicateCleanupHostedService>();
builder.Services.AddHostedService<DataLifecyclePruningHostedService>();
builder.Services.AddOptions<VoiceOptions>()
    .Bind(builder.Configuration.GetSection(VoiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<WeatherInterpretationOptions>()
    .Bind(builder.Configuration.GetSection(WeatherInterpretationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection(MqttOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<ChartQueryOptions>()
    .Bind(builder.Configuration.GetSection(ChartQueryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<ApiKeyMutationEndpointFilter>();
builder.Services.AddOptions<DataLifecycleOptions>()
    .Bind(builder.Configuration.GetSection(DataLifecycleOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var mqttOpts = builder.Configuration.GetSection(MqttOptions.SectionName).Get<MqttOptions>() ?? new MqttOptions();
if (mqttOpts.Enabled && mqttOpts.EnableInApiHost)
{
    builder.Services.AddHostedService<MqttIngestionHostedService>();
}

builder.Services.AddCors(options =>
{
    var corsOpts = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    options.AddDefaultPolicy(policy =>
    {
        if (corsOpts.AllowedOrigins.Length == 0)
        {
            policy.WithOrigins("http://localhost:5000", "http://127.0.0.1:5000")
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.WithOrigins(corsOpts.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
    await dbContext.Database.MigrateAsync();
    await SqlitePragmas.ApplyWalAsync(dbContext);
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/meta/deploy", () => Results.Json(new { deployId = DeployInfo.DeployId }));
app.MapGet("/api/meta/ingest", (IMqttIngestTelemetry telemetry) =>
{
    var s = telemetry.GetSnapshot();
    return Results.Ok(new
    {
        brokerMessages = s.BrokerMessagesReceived,
        skippedTopics = s.TopicsSkippedNonSensor,
        readingsPersisted = s.ReadingsPersisted
    });
});
app.MapGet("/health/live", () => Results.Ok(new { status = "live", utc = DateTime.UtcNow }));
app.MapGet("/health/ready", async (GreenhouseDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "ready", db = "ok" })
        : Results.Problem("Database connection unavailable.", statusCode: 503);
});

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
}).AddEndpointFilter<ApiKeyMutationEndpointFilter>();

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
}).AddEndpointFilter<ApiKeyMutationEndpointFilter>();

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
}).AddEndpointFilter<ApiKeyMutationEndpointFilter>();

app.MapPut("/api/sensor/{sensorId:guid}/display-name", async (Guid sensorId, UpdateSensorDisplayNameRequest body, UpdateSensorDisplayNameCommandService command, CancellationToken ct) =>
{
    var dto = await command.ExecuteAsync(sensorId, body.DisplayName, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
}).AddEndpointFilter<ApiKeyMutationEndpointFilter>();

app.MapDelete("/api/sensor/{sensorId:guid}", async (Guid sensorId, DeleteSensorCommandService command, CancellationToken ct) =>
{
    var ok = await command.ExecuteAsync(sensorId, ct);
    return ok ? Results.NoContent() : Results.NotFound(new { error = "Nie znaleziono czujnika." });
}).AddEndpointFilter<ApiKeyMutationEndpointFilter>();

app.MapGet("/api/sensor/health", async (GetSensorHealthQueryService query, CancellationToken ct) =>
{
    var list = await query.ExecuteAsync(ct);
    return Results.Ok(list);
});

// ─── Charts / Analytics ────────────────────────────────────
app.MapGet("/api/chart/moisture", async (
    Guid? nawaId, Guid? sensorId, DateTime? from, DateTime? to,
    GetMoistureSeriesQueryService query, IOptions<ChartQueryOptions> chartOptions, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-24);
    var toUtc = to ?? DateTime.UtcNow;
    var data = await query.ExecuteAsync(nawaId, sensorId, fromUtc, toUtc, ct);
    return Results.Ok(SampleMax(data, chartOptions.Value.MaxPointsPerSeries));
});

app.MapGet("/api/chart/weather", async (
    Guid? nawaId, Guid? sensorId, DateTime? from, DateTime? to,
    GetWeatherSeriesQueryService query, IOptions<ChartQueryOptions> chartOptions, CancellationToken ct) =>
{
    var fromUtc = from ?? DateTime.UtcNow.AddHours(-24);
    var toUtc = to ?? DateTime.UtcNow;
    var data = await query.ExecuteAsync(nawaId, sensorId, fromUtc, toUtc, ct);
    return Results.Ok(SampleMax(data, chartOptions.Value.MaxPointsPerSeries));
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

static IReadOnlyList<T> SampleMax<T>(IReadOnlyList<T> source, int maxPoints)
{
    if (source.Count <= maxPoints || maxPoints <= 0)
        return source;

    var step = (double)source.Count / maxPoints;
    var output = new List<T>(maxPoints);
    for (var i = 0; i < maxPoints; i++)
    {
        var idx = (int)Math.Floor(i * step);
        if (idx >= source.Count)
            idx = source.Count - 1;
        output.Add(source[idx]);
    }

    return output;
}

public partial class Program;
