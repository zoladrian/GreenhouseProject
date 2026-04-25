using Greenhouse.Domain.Sensors;

namespace Greenhouse.Application.Charts;

/// <summary>
/// Określa, które czujniki wchodzą w skład serii prezentowanych na wykresach nawy.
/// Gleba i metryki środowiskowe (temperatura, bateria) pochodzą wyłącznie z czujników przypisanych do nawy,
/// z wyłączeniem czujników pogodowych — te są zawsze globalne (<see cref="Sensor.NawaId"/> = <c>null</c>)
/// i trafiają tylko do serii pogodowych.
/// </summary>
public sealed class NawaChartSensorScope
{
    private readonly IReadOnlyList<Sensor> _sensors;

    public NawaChartSensorScope(IReadOnlyList<Sensor> sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        _sensors = sensors;
    }

    /// <summary>
    /// Identyfikatory do serii wilgotności / temperatury / baterii na karcie nawy:
    /// wyłącznie czujniki przypisane do tej nawy, bez <see cref="SensorKind.Weather"/>.
    /// </summary>
    public IReadOnlyList<Guid> ResolveMoistureEnvironmentAndGlobalWeatherSensorIds(Guid nawaId)
    {
        return _sensors
            .Where(s => s.NawaId == nawaId && s.Kind != SensorKind.Weather)
            .Select(s => s.Id)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Seria pogody (interpretacja deszczu / światła) — wyłącznie czujniki globalne,
    /// aby ten sam fizyczny czujnik nie był dublowany „per nawa”.
    /// </summary>
    public IReadOnlyList<Guid> ResolveGlobalWeatherSensorIds()
    {
        return _sensors
            .Where(s => s.Kind == SensorKind.Weather && s.NawaId is null)
            .Select(s => s.Id)
            .ToList();
    }
}
