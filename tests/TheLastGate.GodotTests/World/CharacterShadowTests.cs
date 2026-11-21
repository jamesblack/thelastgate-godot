using GdUnit4;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class CharacterShadowTests
{
    [TestCase(1999, false)]
    [TestCase(2000, true)]
    [TestCase(16335, true)]
    [TestCase(16336, false)]
    [TestCase(17360, false)]
    [TestCase(17361, true)]
    [TestCase(0, false)]
    public void Casts_OnlyForTheCurrentClientsCharacterSpriteRanges(int spriteNumber, bool casts)
    {
        AssertThat(CharacterShadow.Casts(spriteNumber)).IsEqual(casts);
    }

    [TestCase(64, 96, -32, 24)]
    [TestCase(32, 64, -16, 16)]
    [TestCase(70, 100, -32, 24)]
    [TestCase(20, 20, 0, 0)]
    public void For_SquashesTheSpriteToAQuarterOfItsWholeTilesUnderTheFeet(
        int width,
        int height,
        int left,
        int shadowHeight)
    {
        AssertThat(CharacterShadow.For(width, height)).IsEqual(new CharacterShadow(left, -10, shadowHeight));
    }
}
