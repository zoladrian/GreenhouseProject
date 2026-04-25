using Greenhouse.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Greenhouse.Infrastructure.Hosting;

/// <summary>
/// Po starcie hosta scala duplikaty czujników (nazwa z topicu vs IEEE) na podstawie zapisanych odczytów z JSON.
///
/// Wcześniejsza wersja używała `_ = RunCleanupAsync(...)` w <c>StartAsync</c> — fire-and-forget tłumił wyjątki
/// i nie respektował lifecycle (gdy host zatrzymywał się szybciej niż 4 s, była race condition).
/// Tu używamy <see cref="BackgroundService"/> z opóźnionym pierwszym uruchomieniem oraz retry z eskalacją:
/// kolejne nieudane próby są coraz rzadsze, żeby nie zalać logów ani CPU.
/// </summary>
public sealed class SensorDuplicateCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensorDuplicateCleanupHostedService> _logger;
    private readonly TimeSpan _firstRunDelay;
    private readonly IReadOnlyList<TimeSpan> _retryBackoff;

    public SensorDuplicateCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SensorDuplicateCleanupHostedService> logger)
        : this(
            scopeFactory,
            logger,
            firstRunDelay: TimeSpan.FromSeconds(4),
            retryBackoff: new[]
            {
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30),
            })
    {
    }

    /// <summary>
    /// Konstruktor testowy — pozwala wstrzyknąć krótkie opóźnienia, żeby testy retry/escalation
    /// nie trwały minutami. NIE używać w produkcji.
    /// </summary>
    internal SensorDuplicateCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SensorDuplicateCleanupHostedService> logger,
        TimeSpan firstRunDelay,
        IReadOnlyList<TimeSpan> retryBackoff)
    {
        if (retryBackoff is null || retryBackoff.Count == 0)
            throw new ArgumentException("Retry backoff musi mieć przynajmniej jeden element.", nameof(retryBackoff));
        _scopeFactory = scopeFactory;
        _logger = logger;
        _firstRunDelay = firstRunDelay;
        _retryBackoff = retryBackoff;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(_firstRunDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                var delay = _retryBackoff[Math.Min(attempt, _retryBackoff.Count - 1)];
                attempt++;
                _logger.LogWarning(
                    ex,
                    "Czyszczenie duplikatów czujników nie powiodło się (próba {Attempt}). Ponawiam za {DelayMin} min.",
                    attempt,
                    delay.TotalMinutes);

                if (attempt >= _retryBackoff.Count)
                {
                    _logger.LogError(
                        ex,
                        "Czyszczenie duplikatów czujników nie udało się {Attempts} razy z rzędu — eskalacja. Backoff zatrzymany na {DelayMin} min.",
                        attempt,
                        delay.TotalMinutes);
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var merger = scope.ServiceProvider.GetRequiredService<ISensorDuplicateMerger>();
        var readings = scope.ServiceProvider.GetRequiredService<ISensorReadingRepository>();
        var result = await merger.MergeAsync(cancellationToken);
        if (result.MergedLegacySensors > 0 || result.RekeyedLegacySensors > 0)
        {
            _logger.LogInformation(
                "Czyszczenie duplikatów czujników zakończone: scalenia={Merges}, rekey na IEEE={Rekeys}",
                result.MergedLegacySensors,
                result.RekeyedLegacySensors);
        }

        var aligned = await readings.AlignAllLinkedReadingSensorIdentifiersAsync(cancellationToken);
        if (aligned > 0)
        {
            _logger.LogInformation(
                "Zsynchronizowano identyfikator w {Count} odczytach z ExternalId czujników (friendly name nie rozszczepia historii).",
                aligned);
        }
    }
}
