using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Map;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class SpriteColoursTests
{
    [TestCase]
    public void Average_TakesTheMeanOfEachChannelRoundingDown()
    {
        byte[] pixels = [10, 20, 30, 255, 11, 21, 31, 255];

        AssertThat(SpriteColours.Average(pixels)).IsEqual(MinimapColours.FromRgb(10, 20, 30));
    }

    [TestCase]
    public void Average_SkipsMagentaAndTransparentPixels()
    {
        byte[] pixels = [255, 0, 255, 255, 1, 2, 3, 0, 40, 50, 60, 255];

        AssertThat(SpriteColours.Average(pixels)).IsEqual(MinimapColours.FromRgb(40, 50, 60));
    }

    [TestCase]
    public void Average_OfARegion_CountsOnlyItsPixels()
    {
        // A 3x2 image; the region is the right-hand 2x1 of the bottom row.
        byte[] pixels =
        [
            200, 200, 200, 255, 200, 200, 200, 255, 200, 200, 200, 255,
            200, 200, 200, 255, 10, 20, 30, 255, 12, 22, 32, 255,
        ];

        AssertThat(SpriteColours.Average(pixels, 3, new PixelRect(1, 1, 2, 1)))
            .IsEqual(MinimapColours.FromRgb(11, 21, 31));
    }

    [TestCase]
    public void Average_OfANothingButSeeThroughSprite_IsBlack()
    {
        byte[] pixels = [255, 0, 255, 255, 9, 9, 9, 0];

        AssertThat(SpriteColours.Average(pixels)).IsEqual(0u);
    }
}
