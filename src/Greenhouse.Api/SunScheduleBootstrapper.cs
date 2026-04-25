using Greenhouse.Application.Weather;

namespace Greenhouse.Api;

internal static class SunScheduleBootstrapper
{
    public static async Task TryImportDefaultScheduleAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveCsvPath();
        if (path is null || !File.Exists(path))
        {
            logger.LogInformation("Sun schedule CSV not found, skipping bootstrap import.");
            return;
        }

        try
        {
            var csv = await File.ReadAllTextAsync(path, cancellationToken);
            using var scope = services.CreateScope();
            var weather = scope.ServiceProvider.GetRequiredService<WeatherControlConfigService>();
            var result = await weather.ImportCsvAsync(csv, cancellationToken);
            logger.LogInformation(
                "Sun schedule bootstrap import done from {Path}. Imported={Imported}, Ignored={Ignored}",
                path,
                result.ImportedRows,
                result.IgnoredRows);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sun schedule bootstrap import failed.");
        }
    }

    private static string? ResolveCsvPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", "sun-schedule", "szczecin_sunrise_sunset_2026.csv"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data", "sun-schedule", "szczecin_sunrise_sunset_2026.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "sun-schedule", "szczecin_sunrise_sunset_2026.csv"),
            "/app/data/sun-schedule/szczecin_sunrise_sunset_2026.csv"
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
