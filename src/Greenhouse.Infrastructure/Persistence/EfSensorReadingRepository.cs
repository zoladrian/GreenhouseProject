using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Greenhouse.Domain.SensorReadings;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfSensorReadingRepository : ISensorReadingRepository
{
    private readonly GreenhouseDbContext _dbContext;
    private readonly IMqttPayloadParser _payloadParser;

    public EfSensorReadingRepository(GreenhouseDbContext dbContext, IMqttPayloadParser payloadParser)
    {
        _dbContext = dbContext;
        _payloadParser = payloadParser;
    }

    public async Task AddAsync(SensorReading reading, CancellationToken cancellationToken)
    {
        await _dbContext.SensorReadings.AddAsync(reading, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestAsync(int count, CancellationToken cancellationToken)
    {
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetBySensorIdsAsync(
        IReadOnlyList<Guid> sensorIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorId != null
                        && sensorIds.Contains(r.SensorId.Value)
                        && r.ReceivedAtUtc >= from
                        && r.ReceivedAtUtc <= to)
            .OrderBy(r => r.ReceivedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestPerSensorAsync(
        IReadOnlyList<Guid> sensorIds,
        CancellationToken cancellationToken)
    {
        if (sensorIds.Count == 0)
        {
            return [];
        }

        var latestTimestamps = _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorId.HasValue && sensorIds.Contains(r.SensorId.Value))
            .GroupBy(r => r.SensorId!.Value)
            .Select(g => new
            {
                SensorId = g.Key,
                LatestReceivedAtUtc = g.Max(x => x.ReceivedAtUtc)
            });

        var rows = await (from r in _dbContext.SensorReadings.AsNoTracking()
                          join lt in latestTimestamps
                              on new { SensorId = r.SensorId!.Value, r.ReceivedAtUtc }
                              equals new { lt.SensorId, ReceivedAtUtc = lt.LatestReceivedAtUtc }
                          select r)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.SensorId)
            .Select(g => g.OrderByDescending(x => x.Id).First())
            .ToList();
    }

    public async Task<string?> TryGetNormalizedIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken cancellationToken)
    {
        var readings = await _dbContext.SensorReadings.AsNoTracking()
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var r in readings)
        {
            var parsed = _payloadParser.ParseSensorPayload(r.RawPayloadJson);
            if (ZigbeeIeeeAddress.TryNormalize(parsed.IeeeAddress, out var n))
                return n;
        }

        return null;
    }

    public async Task<int> AlignSensorIdentifierForSensorAsync(
        Guid sensorId,
        string externalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return 0;

        var trimmed = externalId.Trim();
        return await _dbContext.SensorReadings
            .Where(r => r.SensorId == sensorId && r.SensorIdentifier != trimmed)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.SensorIdentifier, trimmed),
                cancellationToken);
    }

    public async Task<int> AlignAllLinkedReadingSensorIdentifiersAsync(CancellationToken cancellationToken)
    {
        // Jedna linia z Sensors.ExternalId — nie zależy od nazwy z topicu MQTT (friendly name).
        return await _dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE SensorReadings
            SET SensorIdentifier = (SELECT ExternalId FROM Sensors WHERE Id = SensorReadings.SensorId)
            WHERE SensorId IS NOT NULL
              AND EXISTS (
                SELECT 1 FROM Sensors s
                WHERE s.Id = SensorReadings.SensorId
                  AND s.ExternalId != SensorReadings.SensorIdentifier)
            """,
            cancellationToken);
    }

    public async Task<bool> ExistsDuplicateAsync(
        string sensorIdentifier,
        DateTime receivedAtUtc,
        string topic,
        string rawPayloadJson,
        CancellationToken cancellationToken)
    {
        var from = receivedAtUtc.AddSeconds(-2);
        var to = receivedAtUtc.AddSeconds(2);
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .AnyAsync(r =>
                r.SensorIdentifier == sensorIdentifier &&
                r.Topic == topic &&
                r.RawPayloadJson == rawPayloadJson &&
                r.ReceivedAtUtc >= from &&
                r.ReceivedAtUtc <= to,
                cancellationToken);
    }
}
