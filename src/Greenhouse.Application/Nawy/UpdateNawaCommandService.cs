using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Nawy;

public sealed class UpdateNawaCommandService
{
    private readonly INawaRepository _nawy;

    public UpdateNawaCommandService(INawaRepository nawy)
    {
        _nawy = nawy;
    }

    public async Task<NawaDto?> ExecuteAsync(
        Guid id,
        string name,
        string? description,
        string? plantNote,
        bool isActive,
        decimal? moistureMin,
        decimal? moistureMax,
        decimal? temperatureMin,
        decimal? temperatureMax,
        CancellationToken cancellationToken)
    {
        var entity = await _nawy.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Rename(name);
        entity.UpdateDescription(description);
        entity.SetPlantNote(plantNote);
        entity.SetActive(isActive);
        entity.UpdateMoistureThresholds(moistureMin, moistureMax);
        entity.UpdateTemperatureThresholds(temperatureMin, temperatureMax);

        await _nawy.SaveChangesAsync(cancellationToken);
        return NawaMapper.ToDto(entity);
    }
}
