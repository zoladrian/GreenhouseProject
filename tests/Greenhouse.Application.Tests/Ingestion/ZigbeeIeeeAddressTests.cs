using Greenhouse.Application.Ingestion;

namespace Greenhouse.Application.Tests.Ingestion;

public sealed class ZigbeeIeeeAddressTests
{
    [Theory]
    [InlineData("0xFFFFB40E0605C41C", "0xffffb40e0605c41c")]
    [InlineData("ffffb40e0605c41c", "0xffffb40e0605c41c")]
    [InlineData("0xffffb40e0605c41c", "0xffffb40e0605c41c")]
    public void TryNormalize_AcceptsSixteenHexDigits(string raw, string expected)
    {
        Assert.True(ZigbeeIeeeAddress.TryNormalize(raw, out var n));
        Assert.Equal(expected, n);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0x123")]
    [InlineData("not-hex")]
    [InlineData("0xGGGGb40e0605c41c")]
    public void TryNormalize_RejectsInvalid(string raw)
    {
        Assert.False(ZigbeeIeeeAddress.TryNormalize(raw, out _));
    }
}
