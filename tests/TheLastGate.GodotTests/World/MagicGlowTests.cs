using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MagicGlowTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsOnly = 0b0000_0010;
    private const int TileIndex = 1000;

    [TestCase]
    public void For_NoGlowWithoutMagic()
    {
        AssertThat(MagicGlow.For(Tile(0))).IsNull();
    }

    [TestCase(22, true, false, false)]
    [TestCase(25, false, true, false)]
    [TestCase(28, false, false, true)]
    public void For_TakesEachColourFromItsOwnMagicBits(int shift, bool red, bool green, bool blue)
    {
        AssertThat(MagicGlow.For(Tile(3u << shift))).IsEqual(new MagicGlow(red, green, blue, 3));
    }

    [TestCase]
    public void For_MixesColoursAndTakesTheStrongestCount()
    {
        AssertThat(MagicGlow.For(Tile((2u << 22) | (5u << 28)))).IsEqual(new MagicGlow(true, false, true, 5));
        AssertThat(MagicGlow.For(Tile((7u << 22) | (1u << 25) | (4u << 28))))
            .IsEqual(new MagicGlow(true, true, true, 7));
    }

    private static MapTile Tile(uint flags)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(TestCommand.Of(AbsoluteSetMap).Byte(FlagsOnly).UInt16(TileIndex).UInt32(flags)));
        return state.Map.Tiles[TileIndex];
    }
}
