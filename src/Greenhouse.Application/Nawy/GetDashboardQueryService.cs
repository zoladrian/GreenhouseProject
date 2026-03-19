using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Nawy;

namespace Greenhouse.Application.Nawy;

public sealed class GetDashboardQueryService
{
    private readonly INawaRepository _nawy;
    private readonly ISensorRepository _sensors;
    private readonly ISensorReadingRepository _readings;

    public GetDashboardQueryService(
        INawaRepository nawy,
        ISensorRepository sensors,
        ISensorReadingRepository readings)
    {
        _nawy = nawy;
        _sensors = sensors;
        _readings = readings;
    }

    public async Task<IReadOnlyList<NawaSnapshotDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var nawy = await _nawy.ListAsync(cancellationToken);
        var allSensors = await _sensors.ListAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;

        var snapshots = new List<NawaSnapshotDto>();

        foreach (var nawa in nawy.Where(n => n.IsActive))
        {
            var nawaSensors = allSensors.Where(s => s.NawaId == nawa.Id).ToList();
            var sensorIds = nawaSensors.Select(s => s.Id).ToList();

            if (sensorIds.Count == 0)
            {
                snapshots.Add(BuildEmptySnapshot(nawa, utcNow));
                continue;
            }

            var latestReadings = await _readings.GetLatestPerSensorAsync(sensorIds, cancellationToken);

            var moistures = latestReadings
                .Where(r => r.SoilMoisture.HasValue)
                .Select(r => r.SoilMoisture!.Value)
                .ToList();

            var temperatures = latestReadings
                .Where(r => r.Temperature.HasValue)
                .Select(r => r.Temperature!.Value)
                .ToList();

            var batteries = latestReadings
                .Where(r => r.Battery.HasValue)
                .Select(r => r.Battery!.Value)
                .ToList();

            var avgMoisture = moistures.Count > 0 ? Math.Round(moistures.Average(), 2) : (decimal?)null;
            var minMoisture = moistures.Count > 0 ? moistures.Min() : (decimal?)null;
            var maxMoisture = moistures.Count > 0 ? moistures.Max() : (decimal?)null;
            var avgTemp = temperatures.Count > 0 ? Math.Round(temperatures.Average(), 2) : (decimal?)null;
            var lowestBattery = batteries.Count > 0 ? batteries.Min() : (int?)null;
            var oldestReading = latestReadings.Count > 0
                ? latestReadings.Min(r => r.ReceivedAtUtc)
                : (DateTime?)null;

            var status = OperatorStatusCalculator.Calculate(
                sensorIds.Count,
                avgMoisture,
                oldestReading,
                nawa.GetMoistureThresholds(),
                utcNow);

            snapshots.Add(new NawaSnapshotDto(
                nawa.Id, nawa.Name, nawa.PlantNote,
                status, sensorIds.Count,
                avgMoisture, minMoisture, maxMoisture, avgTemp,
                lowestBattery, oldestReading, utcNow));
        }

        return snapshots;
    }

    private static NawaSnapshotDto BuildEmptySnapshot(Nawa nawa, DateTime utcNow) =>
        new(nawa.Id, nawa.Name, nawa.PlantNote,
            OperatorStatus.NoData, 0,
            null, null, null, null,
            null, null, utcNow);
}
