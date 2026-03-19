using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Readings;

public sealed class GetLatestReadingsQueryService
{
    private readonly ISensorReadingRepository _repository;

    public GetLatestReadingsQueryService(ISensorReadingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SensorReadingDto>> ExecuteAsync(int count, CancellationToken cancellationToken)
    {
        var safeCount = Math.Clamp(count, 1, 500);
        var readings = await _repository.GetLatestAsync(safeCount, cancellationToken);

        return readings
            .Select(r => new SensorReadingDto(
                r.Id,
                r.SensorIdentifier,
                r.ReceivedAtUtc,
                r.Topic,
                r.RawPayloadJson,
                r.SoilMoisture,
                r.Temperature,
                r.Battery,
                r.LinkQuality))
            .ToList();
    }
}
