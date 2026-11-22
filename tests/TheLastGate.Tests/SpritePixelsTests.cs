using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class SpritePixelsTests
{
    [Fact]
    public void ApplyMagentaKey_ClearsOnlyOpaqueMagenta()
    {
        byte[] rgba = [255, 0, 255, 255, 255, 0, 255, 128, 255, 1, 255, 255];

        SpritePixels.ApplyMagentaKey(rgba);

        Assert.Equal(new byte[] { 255, 0, 255, 0, 255, 0, 255, 128, 255, 1, 255, 255 }, rgba);
    }
}
