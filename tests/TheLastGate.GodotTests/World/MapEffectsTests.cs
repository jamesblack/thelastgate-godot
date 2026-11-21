using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MapEffectsTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsAndCharacterLook = 0b0010_0010;
    private const int TileIndex = 1000;

    [TestCase(MapFlags.Injured, 1079)]
    [TestCase(MapFlags.Injured | MapFlags.Injured1, 1080)]
    [TestCase(MapFlags.Injured | MapFlags.Injured2, 1081)]
    [TestCase(MapFlags.Injured | MapFlags.Injured1 | MapFlags.Injured2, 1082)]
    [TestCase(MapFlags.Injured | MapFlags.CriticallyHit, 1206)]
    [TestCase(MapFlags.Injured | MapFlags.Injured1 | MapFlags.Injured2 | MapFlags.CriticallyHit, 1209)]
    [TestCase(MapFlags.Injured1, 0)]
    [TestCase(MapFlags.None, 0)]
    public void InjurySprite_PicksTheMarkForTheInjuryBits(MapFlags flags, int sprite)
    {
        AssertThat(MapEffects.InjurySprite(Tile(flags))).IsEqual(sprite);
    }

    [TestCase(1u, 280)]
    [TestCase(18u, 297)]
    [TestCase(0u, 0)]
    public void DeathSprite_StepsThroughTheDeathAnimation(uint step, int sprite)
    {
        AssertThat(MapEffects.DeathSprite(Tile((MapFlags)(step << 17)))).IsEqual(sprite);
    }

    [TestCase]
    public void DeathFollowsCharacter_OnlyWhileACharacterIsDrawnThere()
    {
        AssertThat(MapEffects.DeathFollowsCharacter(Tile(MapFlags.None, 3000))).IsTrue();
        AssertThat(MapEffects.DeathFollowsCharacter(Tile(MapFlags.None))).IsFalse();
    }

    [TestCase(1u, 240)]
    [TestCase(28u, 267)]
    [TestCase(0u, 0)]
    public void TombSprite_StepsThroughTheGraveRising(uint step, int sprite)
    {
        AssertThat(MapEffects.TombSprite(Tile((MapFlags)(step << 12)))).IsEqual(sprite);
    }

    private static MapTile Tile(MapFlags flags, int character = 0)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
                TestCommand.Of(AbsoluteSetMap)
                    .Byte(FlagsAndCharacterLook)
                    .UInt16(TileIndex)
                    .UInt32((uint)flags)
                    .UInt16((ushort)character)
                    .Bytes(0, 0)));
        return state.Map.Tiles[TileIndex];
    }
}
