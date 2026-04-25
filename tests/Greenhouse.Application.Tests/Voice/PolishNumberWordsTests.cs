using Greenhouse.Application.Voice;

namespace Greenhouse.Application.Tests.Voice;

public sealed class PolishNumberWordsTests
{
    [Theory]
    [InlineData(0, "zero")]
    [InlineData(1, "jeden")]
    [InlineData(5, "pięć")]
    [InlineData(11, "jedenaście")]
    [InlineData(15, "piętnaście")]
    [InlineData(20, "dwadzieścia")]
    [InlineData(21, "dwadzieścia jeden")]
    [InlineData(25, "dwadzieścia pięć")]
    [InlineData(30, "trzydzieści")]
    [InlineData(99, "dziewięćdziesiąt dziewięć")]
    [InlineData(100, "sto")]
    [InlineData(101, "sto jeden")]
    [InlineData(123, "sto dwadzieścia trzy")]
    [InlineData(999, "dziewięćset dziewięćdziesiąt dziewięć")]
    [InlineData(1000, "tysiąc")]
    [InlineData(1001, "tysiąc jeden")]
    [InlineData(2000, "dwa tysiące")]
    [InlineData(2025, "dwa tysiące dwadzieścia pięć")]
    [InlineData(5000, "pięć tysięcy")]
    public void ToPolishWords_Cardinal(int n, string expected)
    {
        Assert.Equal(expected, PolishNumberWords.ToPolishWords(n));
    }

    [Fact]
    public void ToPolishWords_Negative_PrefixesMinus()
    {
        Assert.Equal("minus pięć", PolishNumberWords.ToPolishWords(-5));
    }

    [Fact]
    public void ToPolishWords_AboveCap_FallsBackToDigits()
    {
        Assert.Equal("12345", PolishNumberWords.ToPolishWords(12345));
    }

    [Theory]
    [InlineData(0, "zero minut")]
    [InlineData(1, "jedna minuta")]
    [InlineData(2, "dwie minuty")]
    [InlineData(3, "trzy minuty")]
    [InlineData(4, "cztery minuty")]
    [InlineData(5, "pięć minut")]
    [InlineData(11, "jedenaście minut")]
    [InlineData(12, "dwanaście minut")]
    [InlineData(21, "dwadzieścia jeden minut")]
    [InlineData(22, "dwadzieścia dwa minuty")]
    [InlineData(25, "dwadzieścia pięć minut")]
    [InlineData(101, "sto jeden minut")]
    [InlineData(102, "sto dwa minuty")]
    public void MinutesPhrase(int n, string expected)
    {
        Assert.Equal(expected, PolishNumberWords.MinutesPhrase(n));
    }

    [Theory]
    [InlineData(0, "zero dni")]
    [InlineData(1, "jeden dzień")]
    [InlineData(2, "dwa dni")]
    [InlineData(5, "pięć dni")]
    [InlineData(30, "trzydzieści dni")]
    [InlineData(365, "trzysta sześćdziesiąt pięć dni")]
    public void DaysPhrase(int n, string expected)
    {
        Assert.Equal(expected, PolishNumberWords.DaysPhrase(n));
    }
}
