using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class FloorSpriteTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FloorOnly = 0b0000_0001;
    private const int TileIndex = 10 * WorldMap.Size + 10;
    private const int ShimmeringFloor = 16980;

    [TestCase(6160, ShimmeringFloor)]
    [TestCase(6167, ShimmeringFloor + 3)]
    [TestCase(6169, ShimmeringFloor + 4)]
    public void FloorSprite_FlickersTheShimmeringFloorWhenItsTurnComes(int clientTick, int sprite)
    {
        AssertThat(MapSpriteRules.FloorSprite(FloorAt100By200(ShimmeringFloor), clientTick)).IsEqual(sprite);
    }

    [TestCase(0)]
    [TestCase(6170)]
    public void FloorSprite_LeavesTheShimmeringFloorStillAtOtherTimes(int clientTick)
    {
        AssertThat(MapSpriteRules.FloorSprite(FloorAt100By200(ShimmeringFloor), clientTick)).IsEqual(ShimmeringFloor);
    }

    [TestCase]
    public void FloorSprite_DoesNotFlickerOtherFloors()
    {
        AssertThat(MapSpriteRules.FloorSprite(FloorAt100By200(1002), 6167)).IsEqual(1002);
    }

    private static MapTile FloorAt100By200(int floor)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
                TestCommand.Of(ServerCommand.SetOrigin).UInt16(90).UInt16(190),
                TestCommand.Of(AbsoluteSetMap).Byte(FloorOnly).UInt16(TileIndex).UInt16((ushort)floor)));
        return state.Map.Tiles[TileIndex];
    }
}
