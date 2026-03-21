namespace Greenhouse.Application.Abstractions;

/// <param name="TopicFriendlyName">Fragment topicu <c>zigbee2mqtt/&lt;nazwa&gt;</c> (aktualny friendly name w Z2M).</param>
/// <param name="CanonicalExternalId">Preferowany klucz: znormalizowany IEEE z JSON, w przeciwnym razie jak topic.</param>
public sealed record EnsureSensorInput(string TopicFriendlyName, string CanonicalExternalId);
