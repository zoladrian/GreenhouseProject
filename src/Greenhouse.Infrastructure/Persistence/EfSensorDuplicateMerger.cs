using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfSensorDuplicateMerger : ISensorDuplicateMerger
{
    private const int MaxPasses = 25;

    private readonly GreenhouseDbContext _db;
    private readonly IMqttPayloadParser _parser;
    private readonly ILogger<EfSensorDuplicateMerger> _logger;

    public EfSensorDuplicateMerger(
        GreenhouseDbContext db,
        IMqttPayloadParser parser,
        ILogger<EfSensorDuplicateMerger> logger)
    {
        _db = db;
        _parser = parser;
        _logger = logger;
    }

    public async Task<SensorDuplicateMergeResult> MergeAsync(CancellationToken cancellationToken)
    {
        var merges = 0;
        var rekeys = 0;

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var all = await _db.Sensors.AsNoTracking().ToListAsync(cancellationToken);
            var nonCanonical = all
                .Where(s => !ZigbeeIeeeAddress.IsCanonicalStoredExternalId(s.ExternalId))
                .ToList();

            if (nonCanonical.Count == 0)
                break;

            var changedThisPass = false;

            foreach (var legacySnapshot in nonCanonical)
            {
                var ieee = await TryResolveIeeeFromLatestReadingAsync(legacySnapshot.Id, cancellationToken);
                if (ieee is null)
                    continue;

                var master = await _db.Sensors.AsNoTracking()
                    .SingleOrDefaultAsync(s => s.ExternalId == ieee, cancellationToken);

                if (master is not null && master.Id != legacySnapshot.Id)
                {
                    _logger.LogInformation(
                        "Scalanie duplikatu czujnika: legacyId={LegacyId} ({LegacyExt}) → masterId={MasterId} ({MasterExt})",
                        legacySnapshot.Id, legacySnapshot.ExternalId, master.Id, master.ExternalId);
                    await MergeLegacyIntoMasterAsync(legacySnapshot.Id, master.Id, cancellationToken);
                    merges++;
                    changedThisPass = true;
                    continue;
                }

                if (master is null)
                {
                    var legacyTracked = await _db.Sensors
                        .SingleOrDefaultAsync(s => s.Id == legacySnapshot.Id, cancellationToken);
                    if (legacyTracked is null)
                        continue;

                    if (ZigbeeIeeeAddress.IsCanonicalStoredExternalId(legacyTracked.ExternalId))
                        continue;

                    var otherRowHasIeee = await _db.Sensors.AsNoTracking()
                        .AnyAsync(s => s.ExternalId == ieee && s.Id != legacyTracked.Id, cancellationToken);
                    if (otherRowHasIeee)
                        continue;

                    _logger.LogInformation(
                        "Rekey czujnika na IEEE: sensorId={SensorId} {OldExt} → {Ieee}",
                        legacyTracked.Id, legacyTracked.ExternalId, ieee);
                    legacyTracked.RekeyExternalId(ieee);
                    if (string.IsNullOrWhiteSpace(legacyTracked.DisplayName))
                        legacyTracked.SetDisplayName(legacySnapshot.ExternalId);

                    await _db.SaveChangesAsync(cancellationToken);
                    rekeys++;
                    changedThisPass = true;
                }
            }

            if (!changedThisPass)
                break;
        }

        return new SensorDuplicateMergeResult(merges, rekeys);
    }

    private async Task<string?> TryResolveIeeeFromLatestReadingAsync(Guid sensorId, CancellationToken ct)
    {
        var readings = await _db.SensorReadings.AsNoTracking()
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        foreach (var r in readings)
        {
            var parsed = _parser.ParseSensorPayload(r.RawPayloadJson);
            if (ZigbeeIeeeAddress.TryNormalize(parsed.IeeeAddress, out var n))
                return n;
        }

        return null;
    }

    private async Task MergeLegacyIntoMasterAsync(Guid legacyId, Guid masterId, CancellationToken ct)
    {
        await _db.SensorReadings
            .Where(r => r.SensorId == legacyId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.SensorId, masterId), ct);

        var master = await _db.Sensors.SingleAsync(s => s.Id == masterId, ct);
        var legacy = await _db.Sensors.SingleAsync(s => s.Id == legacyId, ct);

        if (master.NawaId is null && legacy.NawaId is not null)
            master.AssignToNawa(legacy.NawaId.Value);

        if (string.IsNullOrWhiteSpace(master.DisplayName) && !string.IsNullOrWhiteSpace(legacy.DisplayName))
            master.SetDisplayName(legacy.DisplayName!);
        else if (string.IsNullOrWhiteSpace(master.DisplayName))
            master.SetDisplayName(legacy.ExternalId);

        _db.Sensors.Remove(legacy);
        await _db.SaveChangesAsync(ct);
    }
}
