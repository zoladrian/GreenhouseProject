using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Ingestion;
using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Sensors;

public sealed class SensorProvisioningService : ISensorProvisioningService
{
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public SensorProvisioningService(ISensorRepository sensors, ISensorReadingRepository readings)
    {
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<SensorEnsureResult> EnsureSensorAsync(EnsureSensorInput input, CancellationToken cancellationToken)
    {
        var canonical = input.CanonicalExternalId.Trim();
        var friendly = input.TopicFriendlyName.Trim();

        var byCanonical = await _sensors.GetByExternalIdForUpdateAsync(canonical, cancellationToken);
        if (byCanonical is not null)
        {
            SyncDisplayNameFromMqtt(byCanonical, friendly);
            await _sensors.SaveChangesAsync(cancellationToken);
            return new SensorEnsureResult(byCanonical.Id, CreatedNew: false);
        }

        var canonicalIsIeee = ZigbeeIeeeAddress.IsCanonicalStoredExternalId(canonical);
        if (canonicalIsIeee && !string.Equals(canonical, friendly, StringComparison.Ordinal))
        {
            var byFriendly = await _sensors.GetByExternalIdForUpdateAsync(friendly, cancellationToken);
            if (byFriendly is not null)
            {
                byFriendly.RekeyExternalId(canonical);
                SyncDisplayNameFromMqtt(byFriendly, friendly);
                await _sensors.SaveChangesAsync(cancellationToken);
                await _readings.AlignSensorIdentifierForSensorAsync(byFriendly.Id, canonical, cancellationToken);
                return new SensorEnsureResult(byFriendly.Id, CreatedNew: false);
            }

            // Zmiana friendly name w Z2M: topic = nowa nazwa, w bazie nadal stary ExternalId z topicu.
            foreach (var snap in (await _sensors.ListAsync(cancellationToken))
                         .Where(s => !ZigbeeIeeeAddress.IsCanonicalStoredExternalId(s.ExternalId)))
            {
                var ieeeFromReading = await _readings.TryGetNormalizedIeeeFromLatestReadingAsync(
                    snap.Id,
                    cancellationToken);
                if (!string.Equals(ieeeFromReading, canonical, StringComparison.Ordinal))
                    continue;

                var legacyTracked = await _sensors.GetByExternalIdForUpdateAsync(snap.ExternalId, cancellationToken);
                if (legacyTracked is null)
                    continue;

                legacyTracked.RekeyExternalId(canonical);
                SyncDisplayNameFromMqtt(legacyTracked, friendly);
                await _sensors.SaveChangesAsync(cancellationToken);
                await _readings.AlignSensorIdentifierForSensorAsync(legacyTracked.Id, canonical, cancellationToken);
                return new SensorEnsureResult(legacyTracked.Id, CreatedNew: false);
            }
        }

        var sensor = Sensor.Register(canonical);
        SyncDisplayNameFromMqtt(sensor, friendly);
        await _sensors.AddAsync(sensor, cancellationToken);
        return new SensorEnsureResult(sensor.Id, CreatedNew: true);
    }

    /// <summary>Ustawia nazwę z aktualnego topicu Z2M (friendly name), żeby UI śledziło zmiany nazw.</summary>
    private static void SyncDisplayNameFromMqtt(Sensor sensor, string mqttFriendlyName)
    {
        if (string.IsNullOrWhiteSpace(mqttFriendlyName))
            return;

        sensor.SetDisplayName(mqttFriendlyName);
    }
}
