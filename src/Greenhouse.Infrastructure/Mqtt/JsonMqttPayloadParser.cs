using System.Text.Json;
using Greenhouse.Application.Abstractions;

namespace Greenhouse.Infrastructure.Mqtt;

public sealed class JsonMqttPayloadParser : IMqttPayloadParser
{
    public ParsedSensorPayload ParseSensorPayload(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        return new ParsedSensorPayload(
            TryGetDecimal(root, "soil_moisture"),
            TryGetDecimal(root, "temperature"),
            TryGetInt(root, "battery"),
            TryGetInt(root, "linkquality"));
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
