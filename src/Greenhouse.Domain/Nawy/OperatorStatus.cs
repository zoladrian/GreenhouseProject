namespace Greenhouse.Domain.Nawy;

public enum OperatorStatus
{
    Ok = 0,
    Warning = 1,
    Dry = 2,
    NoData = 3,
    /// <summary>Min wilgotności poniżej progu „podlej”, a max powyżej „za mokro” — sprzeczne czujniki.</summary>
    Conflict = 4,
    /// <summary>Duży rozstrzał między czujnikami przy braku alarmu sucho/mokro (wg progów).</summary>
    UnevenMoisture = 5,
    /// <summary>
    /// Krótkotrwałe przekroczenie progu „za mokro” (np. świeżo po podlaniu) — poniżej progu czasu uznawania za alarm.
    /// </summary>
    PostWatering = 6,
}
