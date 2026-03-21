using Greenhouse.Application.Abstractions;

namespace Greenhouse.Api;

/// <summary>
/// Po starcie API scala duplikaty czujników (nazwa z topicu vs IEEE) na podstawie zapisanych odczytów z JSON.
/// </summary>
public sealed class SensorDuplicateCleanupHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SensorDuplicateCleanupHostedService> _logger;

    public SensorDuplicateCleanupHostedService(
        IServiceProvider serviceProvider,
        ILogger<SensorDuplicateCleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = RunCleanupAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RunCleanupAsync(CancellationToken appStopping)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), appStopping);
            await using var scope = _serviceProvider.CreateAsyncScope();
            var merger = scope.ServiceProvider.GetRequiredService<ISensorDuplicateMerger>();
            var result = await merger.MergeAsync(appStopping);
            if (result.MergedLegacySensors > 0 || result.RekeyedLegacySensors > 0)
            {
                _logger.LogInformation(
                    "Czyszczenie duplikatów czujników zakończone: scalenia={Merges}, rekey na IEEE={Rekeys}",
                    result.MergedLegacySensors,
                    result.RekeyedLegacySensors);
            }
        }
        catch (OperationCanceledException) when (appStopping.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Czyszczenie duplikatów czujników nie powiodło się (aplikacja działa dalej)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
