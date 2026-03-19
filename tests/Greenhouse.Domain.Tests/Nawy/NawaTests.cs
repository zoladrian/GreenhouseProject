using Greenhouse.Domain.Nawy;

namespace Greenhouse.Domain.Tests.Nawy;

public sealed class NawaTests
{
    [Fact]
    public void Create_ShouldTrimName()
    {
        var nawa = Nawa.Create("  Nawa A  ", null);
        Assert.Equal("Nawa A", nawa.Name);
        Assert.True(nawa.IsActive);
    }

    [Fact]
    public void Create_ShouldRejectEmptyName()
    {
        Assert.Throws<ArgumentException>(() => Nawa.Create("   ", null));
    }

}
