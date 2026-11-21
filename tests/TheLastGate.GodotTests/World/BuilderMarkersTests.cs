using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class BuilderMarkersTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte Flags2Only = 0b0000_0100;
    private const int TileIndex = 1000;

    [TestCase]
    public void For_NothingWithoutTileRules()
    {
        AssertThat(BuilderMarkers.For(Tile(TileRules.None))).IsEmpty();
    }

    [TestCase(TileRules.Bank, 60)]
    [TestCase(TileRules.SightBlock, 84)]
    [TestCase(TileRules.NoExpire, 82)]
    [TestCase(TileRules.UnnamedTopBit, 72)]
    public void For_MarksEachRuleWithItsSprite(TileRules rules, int sprite)
    {
        AssertThat(BuilderMarkers.For(Tile(rules))).ContainsExactly(sprite);
    }

    [TestCase]
    public void For_StacksSeveralRulesInTheCurrentClientsOrder()
    {
        AssertThat(
                BuilderMarkers.For(
                    Tile(TileRules.NoLag | TileRules.NoFight | TileRules.MoveBlock | TileRules.UnnamedTopBit)))
            .ContainsExactly(55, 58, 57, 72);
    }

    [TestCase]
    public void For_EveryRuleTogether()
    {
        var allRules = (TileRules)0x80006FFFu;

        AssertThat(BuilderMarkers.For(Tile(allRules)))
            .ContainsExactly(55, 84, 56, 75, 58, 59, 60, 61, 62, 73, 57, 76, 77, 82, 72);
    }

    private static MapTile Tile(TileRules rules)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(TestCommand.Of(AbsoluteSetMap).Byte(Flags2Only).UInt16(TileIndex).UInt32((uint)rules)));
        return state.Map.Tiles[TileIndex];
    }
}
