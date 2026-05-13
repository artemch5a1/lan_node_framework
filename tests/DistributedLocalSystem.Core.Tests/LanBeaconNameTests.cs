using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;

namespace DistributedLocalSystem.Core.Tests;

public class LanBeaconNameTests
{
    [Fact]
    public void TryBuild_And_TryParse_Roundtrip()
    {
        Assert.True(LanBeaconName.TryBuild("myapp", "node01", out string? full));
        Assert.NotNull(full);
        Assert.True(LanBeaconName.TryParse(full, out LanBeaconParsed p));
        Assert.Equal("myapp", p.ProductSlug);
        Assert.Equal("node01", p.InstanceSlug);
        Assert.Equal(full!, p.FullName);
    }

    [Fact]
    public void TryParse_Rejects_Invalid_Or_Legacy()
    {
        Assert.False(LanBeaconName.TryParse(null, out _));
        Assert.False(LanBeaconName.TryParse("legacy-only", out _));
        Assert.False(LanBeaconName.TryParse("DLSv1-a-b-extra", out _));
    }

    [Fact]
    public void SlugifyLegacy_Normalizes()
    {
        Assert.Equal("myapp26", LanBeaconName.SlugifyLegacy("My App 26!"));
    }

    [Fact]
    public void IsValidSlug_Enforces_Alphabet_And_Length()
    {
        Assert.True(LanBeaconName.IsValidSlug("ab12"));
        Assert.False(LanBeaconName.IsValidSlug("AB"));
        Assert.False(LanBeaconName.IsValidSlug("a-b"));
    }
}
