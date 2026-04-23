using Microsoft.Extensions.Options;

namespace Greenhouse.Application.Weather;

public sealed class WeatherInterpretationOptionsValidator : IValidateOptions<WeatherInterpretationOptions>
{
    public ValidateOptionsResult Validate(string? name, WeatherInterpretationOptions options)
    {
        if (options.DewIntensityMax < 0 ||
            options.LightRainIntensityMin < 0 ||
            options.ModerateRainIntensityMin < 0 ||
            options.HeavyRainIntensityMin < 0)
        {
            return ValidateOptionsResult.Fail("Progi intensywności opadu muszą być >= 0.");
        }

        if (!(options.DewIntensityMax < options.LightRainIntensityMin &&
              options.LightRainIntensityMin <= options.ModerateRainIntensityMin &&
              options.ModerateRainIntensityMin <= options.HeavyRainIntensityMin))
        {
            return ValidateOptionsResult.Fail("Progi opadu muszą być rosnące: Dew < Light <= Moderate <= Heavy.");
        }

        if (!(options.NightLightRawMax <= options.DarkOrOvercastRawMax &&
              options.DarkOrOvercastRawMax <= options.BrightRawMax &&
              options.BrightRawMax <= options.SunnyRawMax))
        {
            return ValidateOptionsResult.Fail("Progi jasności muszą być rosnące: Night <= Dark <= Bright <= Sunny.");
        }

        if (options.DewMaxMinutes <= 0 || options.DewMaxMinutes > 360)
        {
            return ValidateOptionsResult.Fail("DewMaxMinutes musi być w zakresie 1..360.");
        }

        return ValidateOptionsResult.Success;
    }
}
