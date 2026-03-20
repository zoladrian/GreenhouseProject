using System.Text.Json;
using Greenhouse.Application.Abstractions;

namespace Greenhouse.Infrastructure.Mqtt;

public sealed class JsonMqttPayloadParser : IMqttPayloadParser
{
    public ParsedSensorPayload ParseSensorPayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new ParsedSensorPayload(null, null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return new ParsedSensorPayload(null, null, null, null);
            }

            return new ParsedSensorPayload(
                TryGetDecimal(root, "soil_moisture"),
                TryGetDecimal(root, "temperature"),
                TryGetBatteryPercent(root, "battery"),
                TryGetInt(root, "linkquality"));
        }
        catch (JsonException)
        {
            return new ParsedSensorPayload(null, null, null, null);
        }
    }

    /// <summary>Bateria jako int lub ułamek (np. 94.5 %) — zaokrąlenie do całości.</summary>
    private static int? TryGetBatteryPercent(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        if (element.TryGetInt32(out var i))
        {
            return i;
        }

        return element.TryGetDecimal(out var d) ? (int)Math.Round(d) : null;
    }

    private static decimal? TryGetDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value))
        {
            return value;
        }

        return null;
    }

    private static int? TryGetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }
}
