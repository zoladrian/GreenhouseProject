using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Sensors;

public sealed class ListSensorsQueryService
{
    private readonly ISensorRepository _sensors;

    public ListSensorsQueryService(ISensorRepository sensors)
    {
        _sensors = sensors;
    }

    public async Task<IReadOnlyList<SensorListItemDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var list = await _sensors.ListAsync(cancellationToken);
        return list
            .OrderBy(s => s.ExternalId)
            .Select(s => new SensorListItemDto(s.Id, s.ExternalId, s.DisplayName, s.NawaId, s.CreatedAtUtc))
            .ToList();
    }
}
