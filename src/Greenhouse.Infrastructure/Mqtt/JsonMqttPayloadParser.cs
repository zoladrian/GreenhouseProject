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
                TryGetInt(root, "linkquality"),
                TryGetIeeeRaw(root),
                TryGetBool(root, "rain"),
                TryGetDecimal(root, "rain_intensity"),
                TryGetDecimal(root, "illuminance_raw"),
                TryGetDecimal(root, "illuminance_average_20min"),
                TryGetDecimal(root, "illuminance_maximum_today"),
                TryGetBool(root, "cleaning_reminder"));
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

    private static bool? TryGetBool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var i) => i != 0,
            JsonValueKind.String => ParseBoolLike(element.GetString()),
            _ => null
        };
    }

    private static bool? ParseBoolLike(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var t = text.Trim().ToLowerInvariant();
        return t switch
        {
            "1" or "true" or "on" or "yes" or "detected" or "rain" => true,
            "0" or "false" or "off" or "no" or "clear" or "none" => false,
            _ => null
        };
    }

    /// <summary>Wyciąga surowy IEEE z typowych pól Zigbee2MQTT (w tym <c>device</c> przy include_device_information).</summary>
    private static string? TryGetIeeeRaw(JsonElement root)
    {
        if (TryGetStringProperty(root, "ieee_address", out var s))
            return s;
        if (TryGetStringProperty(root, "ieeeAddr", out s))
            return s;

        if (!root.TryGetProperty("device", out var device) || device.ValueKind != JsonValueKind.Object)
            return null;

        if (TryGetStringProperty(device, "ieee_address", out s))
            return s;
        if (TryGetStringProperty(device, "ieeeAddr", out s))
            return s;

        return null;
    }

    private static bool TryGetStringProperty(JsonElement obj, string name, out string value)
    {
        value = string.Empty;
        if (!obj.TryGetProperty(name, out var el))
            return false;
        if (el.ValueKind != JsonValueKind.String)
            return false;
        value = el.GetString() ?? string.Empty;
        return value.Length > 0;
    }
}
