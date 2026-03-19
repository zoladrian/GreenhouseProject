namespace Greenhouse.Application.Abstractions;

public interface IMqttPayloadParser
{
    ParsedSensorPayload ParseSensorPayload(string payloadJson);
}
