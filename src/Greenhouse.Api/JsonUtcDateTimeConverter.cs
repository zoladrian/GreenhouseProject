using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenhouse.Api;

/// <summary>
/// Zapisuje <see cref="DateTime"/> jako UTC w ISO 8601 z „Z”, żeby klient (wykresy) zawsze mógł poprawnie zinterpretować czas.
/// Odczyt zachowuje <see cref="DateTimeKind"/> z formatu wejściowego.
/// </summary>
public sealed class JsonUtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrEmpty(s))
            return default;

        return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        writer.WriteStringValue(utc.ToString("O", CultureInfo.InvariantCulture));
    }
}

public sealed class JsonUtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly JsonUtcDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return Inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        Inner.Write(writer, value.Value, options);
    }
}
