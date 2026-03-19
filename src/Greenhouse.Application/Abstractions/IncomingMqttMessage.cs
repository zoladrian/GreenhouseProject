namespace Greenhouse.Application.Abstractions;

public sealed record IncomingMqttMessage(string Topic, string Payload, DateTime ReceivedAtUtc);
