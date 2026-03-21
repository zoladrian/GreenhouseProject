namespace Greenhouse.Application.Ingestion;

/// <summary>Normalizacja adresu IEEE Zigbee (16 cyfr szesnastkowych) do postaci <c>0x...</c> małymi literami.</summary>
public static class ZigbeeIeeeAddress
{
    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var t = raw.Trim();
        var hex = t.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? t[2..] : t;
        if (hex.Length != 16)
            return false;

        foreach (var c in hex)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        normalized = "0x" + hex.ToLowerInvariant();
        return true;
    }

    /// <summary>
    /// Czy <paramref name="externalId"/> jest już zapisanym, znormalizowanym adresem IEEE (jak w <see cref="TryNormalize"/>).
    /// </summary>
    public static bool IsCanonicalStoredExternalId(string? externalId)
    {
        if (!TryNormalize(externalId, out var normalized))
            return false;

        return string.Equals(normalized, externalId!.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
