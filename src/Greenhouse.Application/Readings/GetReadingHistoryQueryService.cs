using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Readings;

public sealed class GetReadingHistoryQueryService
{
    private readonly ISensorReadingRepository _readings;

    public GetReadingHistoryQueryService(ISensorReadingRepository readings)
    {
        _readings = readings;
    }

    public async Task<IReadOnlyList<SensorReadingDto>> ExecuteAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var readings = await _readings.GetBySensorIdsAsync([sensorId], from, to, cancellationToken);

        return readings
            .OrderBy(r => r.ReceivedAtUtc)
            .Select(r => new SensorReadingDto(
                r.Id, r.SensorIdentifier, r.ReceivedAtUtc,
                r.Topic, r.RawPayloadJson,
                r.SoilMoisture, r.Temperature, r.Battery, r.LinkQuality))
            .ToList();
    }
}
