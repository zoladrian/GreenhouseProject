namespace Greenhouse.Application.Weather;

public sealed record WeatherInterpretationResult(
    RainLevel RainLevel,
    LightLevel LightLevel,
    int RainSignalMinutes,
    string RainReason);
