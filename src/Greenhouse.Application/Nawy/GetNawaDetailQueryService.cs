using Greenhouse.Application.Abstractions;
using Greenhouse.Application.Sensors;

namespace Greenhouse.Application.Nawy;

public sealed record NawaDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? PlantNote,
    bool IsActive,
    decimal? MoistureMin,
    decimal? MoistureMax,
    decimal? TemperatureMin,
    decimal? TemperatureMax,
    DateTime CreatedAtUtc,
    IReadOnlyList<SensorListItemDto> Sensors);

public sealed class GetNawaDetailQueryService
{
    private readonly INawaRepository _nawy;
    private readonly ISensorRepository _sensors;

    public GetNawaDetailQueryService(INawaRepository nawy, ISensorRepository sensors)
    {
        _nawy = nawy;
        _sensors = sensors;
    }

    public async Task<NawaDetailDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nawa = await _nawy.GetByIdAsync(id, cancellationToken);
        if (nawa is null)
        {
            return null;
        }

        var allSensors = await _sensors.ListAsync(cancellationToken);
        var nawaSensors = allSensors
            .Where(s => s.NawaId == nawa.Id)
            .OrderBy(s => s.ExternalId)
            .Select(s => new SensorListItemDto(s.Id, s.ExternalId, s.DisplayName, s.Kind.ToString(), s.NawaId, s.CreatedAtUtc))
            .ToList();

        return new NawaDetailDto(
            nawa.Id, nawa.Name, nawa.Description, nawa.PlantNote,
            nawa.IsActive, nawa.MoistureMin, nawa.MoistureMax,
            nawa.TemperatureMin, nawa.TemperatureMax, nawa.CreatedAtUtc,
            nawaSensors);
    }
}
