namespace Greenhouse.Domain.Nawy;

public sealed record TemperatureThresholds(decimal? Min, decimal? Max)
{
    public static readonly TemperatureThresholds Empty = new(null, null);

    public bool IsBelowMin(decimal value) => Min.HasValue && value < Min.Value;

    public bool IsAboveMax(decimal value) => Max.HasValue && value > Max.Value;
}
