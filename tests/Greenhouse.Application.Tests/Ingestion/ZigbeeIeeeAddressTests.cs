using Greenhouse.Application.Ingestion;

namespace Greenhouse.Application.Tests.Ingestion;

public sealed class ZigbeeIeeeAddressTests
{
    [Theory]
    [InlineData("0x00158d0001a2b3c4")]
    [InlineData("0X00158d0001a2b3c4")]
    public void IsCanonicalStoredExternalId_ShouldReturnTrue_ForNormalizedIeee(string stored)
    {
        Assert.True(ZigbeeIeeeAddress.TryNormalize(stored, out var n));
        Assert.True(ZigbeeIeeeAddress.IsCanonicalStoredExternalId(n));
    }

    [Fact]
    public void IsCanonicalStoredExternalId_ShouldReturnFalse_ForFriendlyName()
    {
        Assert.False(ZigbeeIeeeAddress.IsCanonicalStoredExternalId("Czujnik_1"));
    }
}
