using Greenhouse.Domain.Nawy;

namespace Greenhouse.Application.Nawy;

internal static class NawaMapper
{
    public static NawaDto ToDto(Nawa nawa) =>
        new(nawa.Id, nawa.Name, nawa.Description, nawa.PlantNote,
            nawa.IsActive, nawa.MoistureMin, nawa.MoistureMax,
            nawa.TemperatureMin, nawa.TemperatureMax, nawa.CreatedAtUtc);
}
