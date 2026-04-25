using Greenhouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenhouse.Infrastructure.Hosting;

public sealed class DataLifecyclePruningHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataLifecyclePruningHostedService> _logger;
    private readonly IOptions<DataLifecycleOptions> _options;

    public DataLifecyclePruningHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataLifecyclePruningHostedService> logger,
        IOptions<DataLifecycleOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        if (!opts.EnablePruning)
        {
            _logger.LogInformation("Data pruning is disabled (DataLifecycle:EnablePruning=false).");
            return;
        }

        var interval = TimeSpan.FromHours(opts.PruneIntervalHours);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GreenhouseDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-opts.KeepReadingsDays);
                var deleted = await db.SensorReadings
                    .Where(r => r.ReceivedAtUtc < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "Data pruning removed {Deleted} sensor readings older than {CutoffUtc}.",
                        deleted,
                        cutoff);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Data pruning iteration failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
